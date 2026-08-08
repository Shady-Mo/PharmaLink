namespace Application.Validators.Pharmacy
{
    public class UpdatePharmacyDTOValidator : AbstractValidator<UpdatePharmacyDTO>
    {
        public UpdatePharmacyDTOValidator()
        {
            RuleFor(p => p.OwnerUserId).NotEmpty();

            RuleFor(p => p.VerificationStatus).IsInEnum();

            RuleFor(p => p.LicenseNumber)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(p => p.LegalName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(256);
        }
    }
}
