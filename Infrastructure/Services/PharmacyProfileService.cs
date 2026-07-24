namespace Infrastructure.Services
{
    public class PharmacyProfileService(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<PharmacyProfileService> logger) : IPharmacyProfileService
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxFileSizeBytes = 3 * 1024 * 1024;
        private const string LogoUploadDirectory = "uploads/logos";

        public async Task<Result<PharmacyProfileResponseDto>> GetProfileAsync(
            Guid pharmacyId, CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies
                .AsNoTracking()
                .Where(p => p.PharmacyId == pharmacyId)
                .Select(p => new PharmacyProfileResponseDto
                {
                    PharmacyId = p.PharmacyId,
                    PharmacyName = p.LegalName,
                    LicenseNumber = p.LicenseNumber,
                    LogoUrl = p.LogoUrl,
                    VerificationStatus = p.VerificationStatus
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (pharmacy is null)
            {
                logger.LogWarning("Pharmacy profile not found for PharmacyId {PharmacyId}", pharmacyId);
                return Result.Failure<PharmacyProfileResponseDto>(PharmacyErrors.PharmacyNotFound);
            }

            return Result.Success(pharmacy);
        }

        public async Task<Result<PharmacyProfileResponseDto>> UpdateProfileAsync(
            Guid pharmacyId,
            UpdatePharmacyProfileDto dto,
            CancellationToken cancellationToken = default)
        {
            var pharmacy = await context.Pharmacies
                .FirstOrDefaultAsync(p => p.PharmacyId == pharmacyId, cancellationToken);

            if (pharmacy is null)
            {
                logger.LogWarning("Pharmacy not found for update. PharmacyId: {PharmacyId}", pharmacyId);
                return Result.Failure<PharmacyProfileResponseDto>(PharmacyErrors.PharmacyNotFound);
            }

            pharmacy.LegalName = dto.PharmacyName;

            if (dto.LogoFile is not null && dto.LogoFile.Length > 0)
            {
                var validationResult = ValidateLogoFile(dto.LogoFile);
                if (validationResult is not null)
                    return Result.Failure<PharmacyProfileResponseDto>(validationResult);

                DeleteOldLogo(pharmacy.LogoUrl);

                var relativePath = await SaveLogoFileAsync(dto.LogoFile, cancellationToken);
                pharmacy.LogoUrl = relativePath;
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Pharmacy profile updated successfully. PharmacyId: {PharmacyId}", pharmacyId);

            var response = new PharmacyProfileResponseDto
            {
                PharmacyId = pharmacy.PharmacyId,
                PharmacyName = pharmacy.LegalName,
                LicenseNumber = pharmacy.LicenseNumber,
                LogoUrl = pharmacy.LogoUrl,
                VerificationStatus = pharmacy.VerificationStatus
            };

            return Result.Success(response);
        }

        private static Error? ValidateLogoFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return PharmacyErrors.InvalidLogoType;

            if (file.Length > MaxFileSizeBytes)
                return PharmacyErrors.LogoFileTooLarge;

            return null;
        }

        private async Task<string> SaveLogoFileAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var uploadsPath = Path.Combine(environment.WebRootPath, LogoUploadDirectory);
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                logger.LogInformation("Created logo upload directory: {UploadsPath}", uploadsPath);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsPath, uniqueFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            logger.LogInformation("Logo saved: {FilePath}", fullPath);

            return $"/{LogoUploadDirectory}/{uniqueFileName}";
        }

        private void DeleteOldLogo(string? currentLogoUrl)
        {
            if (string.IsNullOrWhiteSpace(currentLogoUrl))
                return;

            try
            {
                var relativePath = currentLogoUrl.TrimStart('/');
                var fullPath = Path.Combine(environment.WebRootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    logger.LogInformation("Deleted old logo: {FilePath}", fullPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old logo file: {LogoUrl}", currentLogoUrl);
            }
        }
    }
}
