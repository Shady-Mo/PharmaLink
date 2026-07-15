using System.IO;
using System.Linq;
using FluentValidation;
using Application.DTOs.PrescriptionReview.Requests;

namespace Application.Validators.PrescriptionReview;

public class UploadPrescriptionValidator : AbstractValidator<UploadPrescriptionDTO>
{
    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".heic"];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadPrescriptionValidator()
    {
        RuleFor(x => x.Image)
            .NotNull()
                .WithMessage("Prescription image is required.")
            .Must(f => f is not null && f.Length > 0)
                .WithMessage("Uploaded file is empty.")
            .Must(f => f is not null && f.Length <= MaxFileSizeBytes)
                .WithMessage("Image must not exceed 10 MB.")
            .Must(f =>
            {
                var ext = Path.GetExtension(f?.FileName ?? "").ToLowerInvariant();
                return AllowedExtensions.Contains(ext);
            }).WithMessage(
                $"Allowed image types: {string.Join(", ", AllowedExtensions)}.");
    }
}
