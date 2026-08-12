namespace Infrastructure.Services;

public class PatientService(
    AppDbContext context,
    ILogger<PatientService> logger,
    IWebHostEnvironment env) : IPatientService
{
    public async Task<Result<PatientProfileDto>> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // جلب المريض مع العناوين المسجلة باسمه
        var patient = await context.Patients
            .Include(p => p.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Profile fetch failed: Patient with ID {PatientId} was not found.", patientId);
            return Result.Failure<PatientProfileDto>(PatientErrors.PatientNotFound);
        }

        // تحويل البيانات إلى الـ DTO المطلوب مع استخراج إحداثيات الـ Spatial Point بدقة
        var profileDto = new PatientProfileDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            Email = patient.Email ?? string.Empty,
            PhoneNumber = patient.PhoneNumber ?? string.Empty,
            Status = patient.Status.ToString(),
            CreatedAt = patient.CreatedAt,
            ProfilePictureUrl = patient.ProfilePictureUrl,
            Addresses = patient.Addresses.Select(a => new PatientAddressDto
            {
                AddressId = a.AddressId,
                //Label = a.Label,
                AddressLine = a.AddressLine,
                City = a.City,
                Governorate = a.Governorate,
                IsDefault = a.IsDefault,
                Latitude = a.GeoLocation?.Y, // خط العرض من نقطة الـ Spatial
                Longitude = a.GeoLocation?.X // خط الطول من نقطة الـ Spatial
            }).ToList()
        };

        return Result.Success(profileDto);
    }


    public async Task<Result<PatientProfileDto>> UpdateProfileAsync(Guid patientId, UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingByPhone = await context.AppUsers
                .FirstOrDefaultAsync(p => p.PhoneNumber == updateDto.PhoneNumber && p.Id != patientId, cancellationToken);
        if (existingByPhone is not null) {
            logger.LogWarning("Patient tried to update his profile with existing phone: {Phone}", updateDto.PhoneNumber);
            return Result.Failure<PatientProfileDto>(PatientErrors.PhoneAlreadyExists);
        }

        // 1. جلب المريض من قاعدة البيانات مع العناوين المرتبطة به لغرض التعديل (دون استخدام AsNoTracking)
        var patient = await context.Patients
            //.Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Profile update failed: Patient with ID {PatientId} was not found.", patientId);
            return Result.Failure<PatientProfileDto>(PatientErrors.PatientNotFound);
        }

        patient.FullName = updateDto.FullName;
        patient.PhoneNumber = updateDto.PhoneNumber;


        
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Patient profile updated successfully for ID {PatientId}.", patientId);

        var updatedProfileDto = MapToProfileDto(patient);
        return Result.Success(updatedProfileDto);
    }

    private static PatientProfileDto MapToProfileDto(Domain.Entities.Patient patient)
    {
        return new PatientProfileDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            Email = patient.Email ?? string.Empty,
            PhoneNumber = patient.PhoneNumber ?? string.Empty,
            Status = patient.Status.ToString(),
            CreatedAt = patient.CreatedAt,
            ProfilePictureUrl = patient.ProfilePictureUrl,
        };
    }


    public async Task<Result> UploadProfilePictureAsync(Guid patientId, UploadProfilePictureDto dto, string baseUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null)
        {
            logger.LogWarning("Upload profile picture failed: Patient with ID {PatientId} was not found.", patientId);
            return Result.Failure<string>(PatientErrors.PatientNotFound);
        }

        var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileExtension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var absolutePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await dto.Image.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"uploads/profiles/{uniqueFileName}";
        var fullUrl = $"{baseUrl.TrimEnd('/')}/{relativePath}";
        
        // Delete old picture if it exists
        if (!string.IsNullOrEmpty(patient.ProfilePictureUrl))
        {
            try
            {
                var oldRelativePath = patient.ProfilePictureUrl;
                if (Uri.TryCreate(patient.ProfilePictureUrl, UriKind.Absolute, out var uri))
                {
                    oldRelativePath = uri.AbsolutePath.TrimStart('/');
                }
                
                var oldAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldRelativePath);
                if (File.Exists(oldAbsolutePath))
                {
                    File.Delete(oldAbsolutePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old profile picture.");
            }
        }

        patient.ProfilePictureUrl = fullUrl;
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Profile picture uploaded successfully for patient ID {PatientId}.", patientId);
        return Result.Success();
    }

    public async Task<Result<string>> GetProfilePictureUrlAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var patient = await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Get profile picture failed: Patient with ID {PatientId} was not found.", patientId);
            return Result.Failure<string>(PatientErrors.PatientNotFound);
        }

        return Result.Success(patient.ProfilePictureUrl ?? string.Empty);
    }
}