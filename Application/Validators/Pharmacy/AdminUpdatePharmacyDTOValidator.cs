using Application.DTOs.Pharmacy.Request;
using FluentValidation;

namespace Application.Validators.Pharmacy
{
    public class AdminUpdatePharmacyDTOValidator : AbstractValidator<AdminUpdatePharmacyDTO>
    {
        public AdminUpdatePharmacyDTOValidator()
        {
            RuleFor(p => p.VerificationStatus).IsInEnum();

            RuleFor(p => p.LicenseNumber)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(p => p.LegalName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(256);

            RuleFor(p => p.LogoUrl)
                .MaximumLength(1000);
        }
    }
}
