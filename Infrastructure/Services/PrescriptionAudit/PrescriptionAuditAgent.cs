using Application.Services.AI.RAG;

namespace Infrastructure.Services.PrescriptionAudit;

public class PrescriptionAuditAgent(
    IPrescriptionExtractionService extractionService,
    IDrugCatalogPlugin drugCatalogPlugin,
    IAlternativeSearchPlugin alternativeSearchPlugin,
    IAgentProfileProvider agentProfileProvider,
    IPrescriptionAnalyticsRagService ragService,
    AppDbContext context,
    ILogger<PrescriptionAuditAgent> logger)
    : IPrescriptionAuditAgent
{
    public AgentProfile Profile { get; } = agentProfileProvider.GetByCodeName(nameof(PrescriptionAuditAgent))
        ?? throw new InvalidOperationException($"Agent profile '{nameof(PrescriptionAuditAgent)}' was not registered.");

    public async Task<PrescriptionAuditResult> AuditAsync(
        Guid patientUserId,
        string absoluteFilePath,
        string relativeFilePath,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var review = new PrescriptionReview
        {
            PrescriptionReviewId = Guid.NewGuid(),
            PatientUserId = patientUserId,
            PrescriptionImagePath = relativeFilePath,
            OriginalFileName = originalFileName,
            AIModel = "Pending",
            ReviewStatus = PrescriptionReviewStatus.PendingReview,
            ProcessingStatus = PrescriptionProcessingStatus.Processing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.PrescriptionReviews.Add(review);
        await context.SaveChangesAsync(cancellationToken);

        return await ProcessReviewAsync(review, absoluteFilePath, originalFileName, cancellationToken);
    }

    public async Task<PrescriptionAuditResult> ProcessExistingReviewAsync(
        Guid prescriptionReviewId,
        string absoluteFilePath,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var review = await context.PrescriptionReviews
            .Include(r => r.Medicines)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId, cancellationToken);

        if (review is null)
        {
            return new PrescriptionAuditResult
            {
                IsValidPrescription = false,
                ValidationMessage = "Prescription review was not found."
            };
        }

        return await ProcessReviewAsync(review, absoluteFilePath, originalFileName, cancellationToken);
    }

    private async Task<PrescriptionAuditResult> ProcessReviewAsync(
        PrescriptionReview review,
        string absoluteFilePath,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        var file = new AIFileContent
        {
            FileName = originalFileName,
            ContentType = ResolveContentType(absoluteFilePath),
            Content = await File.ReadAllBytesAsync(absoluteFilePath, cancellationToken)
        };

        var aiResult = await extractionService.ExtractAsync(
            file,
            cancellationToken);

        if (!aiResult.IsValidPrescription)
        {
            review.ProcessingStatus = PrescriptionProcessingStatus.Rejected;
            review.ReviewStatus = PrescriptionReviewStatus.Rejected;
            review.ReviewNotes = aiResult.ValidationMessage
                ?? "The uploaded document does not appear to be a valid prescription.";
            review.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new PrescriptionAuditResult
            {
                IsValidPrescription = false,
                ValidationMessage = review.ReviewNotes,
                Review = review
            };
        }

        if (aiResult.IsEmpty)
        {
            review.ProcessingStatus = PrescriptionProcessingStatus.Failed;
            review.ReviewNotes = "No medicines were detected in the uploaded prescription.";
            review.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new PrescriptionAuditResult
            {
                IsValidPrescription = false,
                ValidationMessage = review.ReviewNotes,
                Review = review
            };
        }

        var medicines = new List<PrescriptionReviewMedicine>();

        foreach (var extractedMedicine in aiResult.Medicines)
        {
            var match = await drugCatalogPlugin.FindBestMatchAsync(extractedMedicine, cancellationToken);

            if (match.Status is PrescriptionMedicineMatchStatus.NotFound or PrescriptionMedicineMatchStatus.Unavailable)
            {
                var alternative = await alternativeSearchPlugin.FindAlternativeAsync(
                    extractedMedicine,
                    match.Drug,
                    cancellationToken);

                if (alternative.Status == PrescriptionMedicineMatchStatus.AlternativeSuggested)
                {
                    match = alternative;
                }
            }

            medicines.Add(new PrescriptionReviewMedicine
            {
                PrescriptionReviewMedicineId = Guid.NewGuid(),
                PrescriptionReviewId = review.PrescriptionReviewId,
                MedicineName = extractedMedicine.MedicineName,
                OriginalMedicineName = null,
                GenericName = extractedMedicine.GenericName ?? match.Drug?.GenericName ?? match.SuggestedAlternativeDrug?.GenericName,
                Strength = extractedMedicine.Strength,
                DosageForm = extractedMedicine.DosageForm,
                Dose = extractedMedicine.Dose,
                Frequency = extractedMedicine.Frequency,
                Duration = extractedMedicine.Duration,
                Quantity = extractedMedicine.Quantity,
                Route = extractedMedicine.Route,
                Confidence = extractedMedicine.Confidence,
                MatchedDrugId = match.DrugId,
                SuggestedAlternativeDrugId = match.SuggestedAlternativeDrugId,
                MatchStatus = match.Status,
                MatchReason = match.Reason,
                MatchScore = match.Score,
                RequiresPatientApproval = match.RequiresPatientApproval,
                IsEdited = false
            });
        }

        if (medicines.Any(m => m.RequiresPatientApproval))
        {
            review.ProcessingStatus = PrescriptionProcessingStatus.NeedsPatientApproval;
        }
        else if (medicines.Any(m => m.MatchStatus is PrescriptionMedicineMatchStatus.NotFound
                     or PrescriptionMedicineMatchStatus.Unavailable))
        {
            review.ProcessingStatus = PrescriptionProcessingStatus.PendingPharmacistReview;
        }
        else
        {
            review.ProcessingStatus = PrescriptionProcessingStatus.Completed;
        }

        review.AIModel = aiResult.ModelUsed;
        review.ExtractedText = aiResult.ExtractedText;
        review.AISummary = aiResult.AISummary;
        review.ExtractionConfidence = aiResult.ExtractionConfidence;
        review.ReviewStatus = PrescriptionReviewStatus.PendingReview;
        review.ReviewNotes = null;
        review.UpdatedAt = DateTime.UtcNow;

        if (review.Medicines.Count > 0)
        {
            context.PrescriptionReviewMedicines.RemoveRange(review.Medicines);
        }

        context.PrescriptionReviewMedicines.AddRange(medicines);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await ragService.IndexSinglePrescriptionAsync(review.PrescriptionReviewId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to auto-index prescription review {ReviewId} in vector store.", review.PrescriptionReviewId);
        }

        logger.LogInformation(
            "{AgentName} completed prescription audit for patient {PatientUserId}. Review {ReviewId}",
            Profile.DisplayName,
            review.PatientUserId,
            review.PrescriptionReviewId);

        return new PrescriptionAuditResult
        {
            IsValidPrescription = true,
            Review = review,
            CartId = null,
            Medicines = medicines
        };
    }

    private static string ResolveContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".pdf" => "application/pdf",
            _ => "image/jpeg"
        };
    }
}
