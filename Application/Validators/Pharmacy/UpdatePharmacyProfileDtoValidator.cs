namespace Application.Validators.Pharmacy
{
    public class UpdatePharmacyProfileDtoValidator : AbstractValidator<UpdatePharmacyProfileDto>
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxFileSizeBytes = 3 * 1024 * 1024;

        public UpdatePharmacyProfileDtoValidator()
        {
            RuleFor(x => x.PharmacyName)
                .NotEmpty().WithMessage("Pharmacy name is required.")
                .MinimumLength(3).WithMessage("Pharmacy name must be at least 3 characters.")
                .MaximumLength(256).WithMessage("Pharmacy name must not exceed 256 characters.");

            RuleFor(x => x.LogoFile)
                .Must(file =>
                {
                    if (file is null) return true;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    return AllowedExtensions.Contains(ext);
                })
                .WithMessage($"Logo must be an image file ({string.Join(", ", AllowedExtensions)}).")
                .Must(file => file is null || file.Length <= MaxFileSizeBytes)
                .WithMessage($"Logo file size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }
    }
}
