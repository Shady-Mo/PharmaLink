namespace Application.Validators.Pharmacy
{
    public class AddPharmacyDTOValidator : AbstractValidator<AddPharmacyDTO>
    {
        public AddPharmacyDTOValidator()
        {
            RuleFor(p => p.OwnerUserId).NotEmpty();

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
