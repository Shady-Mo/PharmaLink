using FluentValidation;
using System.Security.Claims;
using Hangfire;
namespace Infrastructure.Services;

public class PrescriptionReviewService(
    AppDbContext context,
    IPrescriptionAuditJobQueue prescriptionAuditJobQueue,
    ICartService cartService,
    IHttpContextAccessor httpContextAccessor,
    IValidator<UploadPrescriptionDTO> uploadValidator,
    IValidator<UpdatePrescriptionReviewDTO> updateValidator,
    IBackgroundJobClient backgroundJobClient,
    ILogger<PrescriptionReviewService> logger,
    IWebHostEnvironment env,
    IWebPushNotificationService pushNotificationService)
    : IPrescriptionReviewService
{
    public async Task<Result<PrescriptionReviewUploadResponseDTO>> UploadAndExtractAsync(
        Guid patientUserId,
        UploadPrescriptionDTO dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await uploadValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First();
            return Result.Failure<PrescriptionReviewUploadResponseDTO>(
                new Error("Validation", firstError.ErrorMessage, StatusCodes.Status400BadRequest));
        }

        // 1. Create uploads folder if it doesn't exist
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "prescriptions");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 2. Generate unique filename and save file
        var fileExtension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var absolutePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await dto.Image.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"uploads/prescriptions/{uniqueFileName}";

        var review = new PrescriptionReview
        {
            PrescriptionReviewId = Guid.NewGuid(),
            PatientUserId = patientUserId,
            PrescriptionImagePath = relativePath,
            OriginalFileName = dto.Image.FileName,
            AIModel = "Pending",
            ReviewStatus = PrescriptionReviewStatus.PendingReview,
            ProcessingStatus = PrescriptionProcessingStatus.Processing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.PrescriptionReviews.Add(review);
        await context.SaveChangesAsync(cancellationToken);

        await prescriptionAuditJobQueue.EnqueueAsync(
            new PrescriptionAuditJob(
                review.PrescriptionReviewId,
                patientUserId,
                absolutePath,
                relativePath,
                dto.Image.FileName),
            cancellationToken);

        // 4. Construct response
        var request = httpContextAccessor.HttpContext?.Request;
        var imageUrl = request != null
            ? $"{request.Scheme}://{request.Host}/{relativePath}"
            : $"/{relativePath}";

        var responseDto = ToUploadResponse(review, cartId: null, imageUrl, []);

        return Result.Success(responseDto);
    }

    public async Task<Result<PaginatedList<PrescriptionReviewSummaryDTO>>> GetAllAsync(
        GetPrescriptionReviewsRequest request,
        Guid userId, // (إذا كانت User.GetUserId() ترجع string، اجعلها string واستخدم Guid.TryParse لو PatientId نوعه Guid)
        string role)
    {
        var query = context.PrescriptionReviews.AsNoTracking();

        // 🎯 التصفية تلقائياً إذا كان المستخدم مريضاً
        if (role == AppRoles.Patient)
        {
            query = query.Where(r => r.PatientUserId == userId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.ReviewStatus == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            bool isGuid = Guid.TryParse(searchTerm, out Guid parsedGuid);

            if (isGuid)
            {
                query = query.Where(r => r.PrescriptionReviewId == parsedGuid ||
                                         (r.Patient != null && r.Patient.FullName.Contains(searchTerm)));
            }
            else
            {
                query = query.Where(r => r.Patient != null && r.Patient.FullName.Contains(searchTerm));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            switch (request.SortBy.ToLower())
            {
                case "patientname":
                    query = request.IsDescending
                        ? query.OrderByDescending(r => r.Patient.FullName)
                        : query.OrderBy(r => r.Patient.FullName);
                    break;
                case "status":
                    query = request.IsDescending
                        ? query.OrderByDescending(r => r.Patient.Status)
                        : query.OrderBy(r => r.Patient.Status);
                    break;
                case "medicinecount":
                    query = request.IsDescending
                        ? query.OrderByDescending(r => r.Medicines.Count)
                        : query.OrderBy(r => r.Medicines.Count);
                    break;
                case "createdat":
                    query = request.IsDescending
                        ? query.OrderByDescending(r => r.CreatedAt)
                        : query.OrderBy(r => r.CreatedAt);
                    break;
                case "reviewedat":
                    query = request.IsDescending
                        ? query.OrderByDescending(r => r.ReviewedAt)
                        : query.OrderBy(r => r.ReviewedAt);
                    break;
                default:
                    query = query.OrderBy(r => r.PrescriptionReviewId);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(r => r.CreatedAt);
        }

        var totalCount = await query.CountAsync();

        var projectedQuery = query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.PrescriptionReviewId,
                PatientName = r.Patient != null ? r.Patient.FullName : "Unknown",
                r.PrescriptionImagePath,
                r.ReviewStatus,
                MedicineCount = r.Medicines.Count,
                r.CreatedAt,
                r.ReviewedAt
            });

        var items = await projectedQuery.ToListAsync();

        var requestHttp = httpContextAccessor.HttpContext?.Request;
        var baseUrl = requestHttp != null
            ? $"{requestHttp.Scheme}://{requestHttp.Host}/"
            : "/";

        var dtos = items.Select(r => new PrescriptionReviewSummaryDTO
        {
            ReviewId = r.PrescriptionReviewId,
            PatientName = r.PatientName,
            ImageUrl = $"{baseUrl}{r.PrescriptionImagePath}",
            Status = r.ReviewStatus.ToString(),
            MedicineCount = r.MedicineCount,
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt
        }).ToList();

        var paginatedList = new PaginatedList<PrescriptionReviewSummaryDTO>(
            dtos,
            request.PageNumber,
            totalCount,
            request.PageSize);

        return Result.Success(paginatedList);
    }

    public async Task<Result<PrescriptionReviewDetailDTO>> GetByIdAsync(
        Guid prescriptionReviewId,
        Guid requestingUserId,
        string requestingUserRole)
    {
        var review = await context.PrescriptionReviews
            .Include(r => r.Patient)
            .Include(r => r.Medicines)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId);

        if (review == null)
        {
            return Result.Failure<PrescriptionReviewDetailDTO>(PrescriptionReviewErrors.NotFound);
        }

        // Guard: Patients can only view their own reviews
        if (requestingUserRole == "Patient" && review.PatientUserId != requestingUserId)
        {
            return Result.Failure<PrescriptionReviewDetailDTO>(PrescriptionReviewErrors.Forbidden);
        }

        var requestHttp = httpContextAccessor.HttpContext?.Request;
        var baseUrl = requestHttp != null ? $"{requestHttp.Scheme}://{requestHttp.Host}/" : "/";

        var detailDto = new PrescriptionReviewDetailDTO
        {
            ReviewId = review.PrescriptionReviewId,
            PatientUserId = review.PatientUserId,
            PatientName = review.Patient?.FullName ?? "Unknown",
            ImageUrl = $"{baseUrl}{review.PrescriptionImagePath}",
            Status = review.ReviewStatus.ToString(),
            ProcessingStatus = review.ProcessingStatus.ToString(),
            AIModel = review.AIModel,
            ReviewNotes = review.ReviewNotes,
            CreatedAt = review.CreatedAt,
            ReviewedAt = review.ReviewedAt,
            CreatedOrderId = review.CreatedOrderId,
            Medicines = review.Medicines.Select(m => new MedicineDetailDTO
            {
                Id = m.PrescriptionReviewMedicineId,
                MedicineName = m.MedicineName,
                OriginalMedicineName = m.OriginalMedicineName,
                GenericName = m.GenericName,
                Strength = m.Strength,
                DosageForm = m.DosageForm,
                Dose = m.Dose,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Quantity = m.Quantity,
                Route = m.Route,
                Confidence = m.Confidence,
                MatchedDrugId = m.MatchedDrugId,
                SuggestedAlternativeDrugId = m.SuggestedAlternativeDrugId,
                MatchStatus = m.MatchStatus.ToString(),
                MatchReason = m.MatchReason,
                MatchScore = m.MatchScore,
                RequiresPatientApproval = m.RequiresPatientApproval,
                PatientApprovedAt = m.PatientApprovedAt,
                IsEdited = m.IsEdited
            }).ToList()
        };

        return Result.Success(detailDto);
    }

    public async Task<Result<PrescriptionReviewDetailDTO>> UpdateMedicinesAsync(
        Guid prescriptionReviewId,
        Guid pharmacistUserId,
        UpdatePrescriptionReviewDTO dto)
    {
        var validationResult = await updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First();
            return Result.Failure<PrescriptionReviewDetailDTO>(
                new Error("Validation", firstError.ErrorMessage, StatusCodes.Status400BadRequest));
        }

        var review = await context.PrescriptionReviews
            .Include(r => r.Patient)
            .Include(r => r.Medicines)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId);

        if (review == null)
        {
            return Result.Failure<PrescriptionReviewDetailDTO>(PrescriptionReviewErrors.NotFound);
        }

        if (review.ReviewStatus != PrescriptionReviewStatus.PendingReview)
        {
            return Result.Failure<PrescriptionReviewDetailDTO>(PrescriptionReviewErrors.AlreadyReviewed);
        }

        // 1. Identify deletions
        var requestedIds = dto.Medicines
            .Where(m => m.PrescriptionReviewMedicineId.HasValue)
            .Select(m => m.PrescriptionReviewMedicineId!.Value)
            .ToList();

        var medicinesToDelete = review.Medicines
            .Where(m => !requestedIds.Contains(m.PrescriptionReviewMedicineId))
            .ToList();

        foreach (var med in medicinesToDelete)
        {
            context.PrescriptionReviewMedicines.Remove(med);
        }

        // 2. Identify updates and inserts
        foreach (var item in dto.Medicines)
        {
            if (item.PrescriptionReviewMedicineId.HasValue)
            {
                var existing = review.Medicines.FirstOrDefault(m =>
                    m.PrescriptionReviewMedicineId == item.PrescriptionReviewMedicineId.Value);
                if (existing == null)
                {
                    return Result.Failure<PrescriptionReviewDetailDTO>(PrescriptionReviewErrors.MedicineNotFound);
                }

                // Check if edited
                var isNameChanged = existing.MedicineName != item.MedicineName;
                var isOtherChanged = existing.GenericName != item.GenericName ||
                                     existing.Strength != item.Strength ||
                                     existing.DosageForm != item.DosageForm ||
                                     existing.Dose != item.Dose ||
                                     existing.Frequency != item.Frequency ||
                                     existing.Duration != item.Duration ||
                                     existing.Quantity != item.Quantity ||
                                     existing.Route != item.Route;

                if (isNameChanged)
                {
                    if (existing.OriginalMedicineName == null)
                    {
                        existing.OriginalMedicineName = existing.MedicineName;
                    }

                    existing.IsEdited = true;
                }
                else if (isOtherChanged)
                {
                    existing.IsEdited = true;
                }

                // Update fields
                existing.MedicineName = item.MedicineName;
                existing.GenericName = item.GenericName;
                existing.Strength = item.Strength;
                existing.DosageForm = item.DosageForm;
                existing.Dose = item.Dose;
                existing.Frequency = item.Frequency;
                existing.Duration = item.Duration;
                existing.Quantity = item.Quantity;
                existing.Route = item.Route;
            }
            else
            {
                // New medicine added by pharmacist
                var newMed = new PrescriptionReviewMedicine
                {
                    PrescriptionReviewMedicineId = Guid.NewGuid(),
                    PrescriptionReviewId = review.PrescriptionReviewId,
                    MedicineName = item.MedicineName,
                    OriginalMedicineName = null, // Added by pharmacist, no AI original name
                    GenericName = item.GenericName,
                    Strength = item.Strength,
                    DosageForm = item.DosageForm,
                    Dose = item.Dose,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Quantity = item.Quantity,
                    Route = item.Route,
                    Confidence = 1.0, // Manual addition is 100% confidence
                    IsEdited = true // Marked as edited/manually handled
                };
                context.PrescriptionReviewMedicines.Add(newMed);
            }
        }

        review.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // 3. Return detail DTO
        var requestHttp = httpContextAccessor.HttpContext?.Request;
        var baseUrl = requestHttp != null ? $"{requestHttp.Scheme}://{requestHttp.Host}/" : "/";

        // Reload to get fresh relations
        await context.Entry(review).Collection(r => r.Medicines).LoadAsync();

        var detailDto = new PrescriptionReviewDetailDTO
        {
            ReviewId = review.PrescriptionReviewId,
            PatientUserId = review.PatientUserId,
            PatientName = review.Patient?.FullName ?? "Unknown",
            ImageUrl = $"{baseUrl}{review.PrescriptionImagePath}",
            Status = review.ReviewStatus.ToString(),
            ProcessingStatus = review.ProcessingStatus.ToString(),
            AIModel = review.AIModel,
            ReviewNotes = review.ReviewNotes,
            CreatedAt = review.CreatedAt,
            ReviewedAt = review.ReviewedAt,
            CreatedOrderId = review.CreatedOrderId,
            Medicines = review.Medicines.Select(m => new MedicineDetailDTO
            {
                Id = m.PrescriptionReviewMedicineId,
                MedicineName = m.MedicineName,
                OriginalMedicineName = m.OriginalMedicineName,
                GenericName = m.GenericName,
                Strength = m.Strength,
                DosageForm = m.DosageForm,
                Dose = m.Dose,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Quantity = m.Quantity,
                Route = m.Route,
                Confidence = m.Confidence,
                MatchedDrugId = m.MatchedDrugId,
                SuggestedAlternativeDrugId = m.SuggestedAlternativeDrugId,
                MatchStatus = m.MatchStatus.ToString(),
                MatchReason = m.MatchReason,
                MatchScore = m.MatchScore,
                RequiresPatientApproval = m.RequiresPatientApproval,
                PatientApprovedAt = m.PatientApprovedAt,
                IsEdited = m.IsEdited
            }).ToList()
        };

        return Result.Success(detailDto);
    }

    //public async Task<Result> ApproveAsync(Guid prescriptionReviewId, Guid pharmacistUserId, ApproveRejectDTO dto)
    //{
    //    var review =
    //        await context.PrescriptionReviews.FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId);
    //    if (review == null)
    //    {
    //        return Result.Failure(PrescriptionReviewErrors.NotFound);
    //    }

    //    if (review.ReviewStatus != PrescriptionReviewStatus.PendingReview)
    //    {
    //        return Result.Failure(PrescriptionReviewErrors.AlreadyReviewed);
    //    }

    //    review.ReviewStatus = PrescriptionReviewStatus.Approved;
    //    review.PharmacistUserId = pharmacistUserId;
    //    review.ReviewedAt = DateTime.UtcNow;
    //    review.ReviewNotes = dto.Notes;
    //    review.UpdatedAt = DateTime.UtcNow;

    //    await context.SaveChangesAsync();
    //    return Result.Success();
    //}

    public async Task<Result> ApproveAsync(Guid prescriptionReviewId, Guid pharmacistUserId, ApproveRejectDTO dto)
    {
        var review = await context.PrescriptionReviews
            .Include(r => r.Medicines)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId);

        if (review == null)
        {
            return Result.Failure(PrescriptionReviewErrors.NotFound);
        }

        if (review.ReviewStatus != PrescriptionReviewStatus.PendingReview)
        {
            return Result.Failure(PrescriptionReviewErrors.AlreadyReviewed);
        }

        review.ReviewStatus = PrescriptionReviewStatus.Approved;
        review.PharmacistUserId = pharmacistUserId;
        review.ReviewedAt = DateTime.UtcNow;
        review.ReviewNotes = dto.Notes;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await pushNotificationService.SendNotificationAsync(
            review.PatientUserId, 
            "تم قبول الروشتة ✅", 
            "تمت مراجعة الروشتة الخاصة بك وقبولها بنجاح.", 
            "/patient/prescriptions");

        backgroundJobClient.Enqueue<IPrescriptionEmbeddingJob>(
            job => job.ProcessAsync(review.PrescriptionReviewId, CancellationToken.None));

        return Result.Success();
    }

    public async Task<Result> RejectAsync(Guid prescriptionReviewId, Guid pharmacistUserId, ApproveRejectDTO dto)
    {
        var review =
            await context.PrescriptionReviews.FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId);
        if (review == null)
        {
            return Result.Failure(PrescriptionReviewErrors.NotFound);
        }

        if (review.ReviewStatus != PrescriptionReviewStatus.PendingReview)
        {
            return Result.Failure(PrescriptionReviewErrors.AlreadyReviewed);
        }

        review.ReviewStatus = PrescriptionReviewStatus.Rejected;
        review.PharmacistUserId = pharmacistUserId;
        review.ReviewedAt = DateTime.UtcNow;
        review.ReviewNotes = dto.Notes;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        
        await pushNotificationService.SendNotificationAsync(
            review.PatientUserId, 
            "عذراً، تم رفض الروشتة ❌", 
            "تم مراجعة الروشتة الخاصة بك وتم رفضها. " + (string.IsNullOrEmpty(dto.Notes) ? "" : $"السبب: {dto.Notes}"), 
            "/patient/prescriptions");
            
        return Result.Success();
    }

    public async Task<Result<CartResponseDTO>> AddMedicinesToCartAsync(
        Guid prescriptionReviewId,
        Guid patientUserId,
        AddPrescriptionReviewMedicinesToCartDTO dto,
        CancellationToken cancellationToken = default)
    {
        var review = await context.PrescriptionReviews
            .Include(r => r.Medicines)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure<CartResponseDTO>(PrescriptionReviewErrors.NotFound);
        }

        if (review.PatientUserId != patientUserId)
        {
            return Result.Failure<CartResponseDTO>(PrescriptionReviewErrors.Forbidden);
        }

        if (review.ReviewStatus != PrescriptionReviewStatus.Approved)
        {
            return Result.Failure<CartResponseDTO>(PrescriptionReviewErrors.NotApproved);
        }

        if (dto.PrescriptionReviewMedicineIds.Count == 0)
        {
            return Result.Failure<CartResponseDTO>(PrescriptionReviewErrors.MedicineNotFound);
        }

        var requestedIds = dto.PrescriptionReviewMedicineIds.ToHashSet();
        var selectedMedicines = review.Medicines
            .Where(m => requestedIds.Contains(m.PrescriptionReviewMedicineId))
            .ToList();

        if (selectedMedicines.Count != requestedIds.Count)
        {
            return Result.Failure<CartResponseDTO>(PrescriptionReviewErrors.MedicineNotFound);
        }

        CartResponseDTO? updatedCart = null;

        foreach (var medicine in selectedMedicines)
        {
            var drugId = ResolvePatientSelectedDrugId(medicine);
            if (!drugId.HasValue)
            {
                return Result.Failure<CartResponseDTO>(PrescriptionReviewErrors.MedicineCannotBeAddedToCart);
            }

            var addResult = await cartService.AddItemAsync(
                patientUserId,
                new AddCartItemRequestDTO
                {
                    DrugId = drugId.Value,
                    Quantity = Math.Max(medicine.Quantity, 1)
                },
                cancellationToken);

            if (!addResult.IsSuccess)
            {
                return addResult;
            }

            updatedCart = addResult.Value;
        }

        return Result.Success(updatedCart!);
    }

    private static Guid? ResolvePatientSelectedDrugId(PrescriptionReviewMedicine medicine)
    {
        if (medicine.MatchStatus is PrescriptionMedicineMatchStatus.ExactMatch
            or PrescriptionMedicineMatchStatus.FuzzyMatch)
        {
            return medicine.MatchedDrugId;
        }

        if (medicine.MatchStatus == PrescriptionMedicineMatchStatus.AlternativeSuggested)
        {
            return medicine.SuggestedAlternativeDrugId;
        }

        return null;
    }

    private static PrescriptionReviewUploadResponseDTO ToUploadResponse(
        PrescriptionReview review,
        Guid? cartId,
        string imageUrl,
        IReadOnlyList<PrescriptionReviewMedicine> medicines)
    {
        return new PrescriptionReviewUploadResponseDTO
        {
            ReviewId = review.PrescriptionReviewId,
            CartId = cartId,
            Status = review.ReviewStatus.ToString(),
            ProcessingStatus = review.ProcessingStatus.ToString(),
            ImageUrl = imageUrl,
            Medicines = medicines.Select(m => new ExtractedMedicineSummaryDTO
            {
                Id = m.PrescriptionReviewMedicineId,
                Name = m.MedicineName,
                Strength = m.Strength,
                DosageForm = m.DosageForm,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Quantity = m.Quantity,
                Confidence = m.Confidence,
                MatchedDrugId = m.MatchStatus == PrescriptionMedicineMatchStatus.AlternativeSuggested
                    ? m.SuggestedAlternativeDrugId
                    : m.MatchedDrugId,
                SuggestedAlternativeDrugId = m.SuggestedAlternativeDrugId,
                Status = m.MatchStatus.ToString(),
                AiNote = m.MatchReason,
                RequiresPatientApproval = m.RequiresPatientApproval
            }).ToList()
        };
    }
}