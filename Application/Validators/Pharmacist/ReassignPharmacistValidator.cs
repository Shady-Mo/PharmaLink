namespace Application.Validators.Pharmacist;

public class ReassignPharmacistValidator : AbstractValidator<ReassignPharmacistRequestDTO>
{
    public ReassignPharmacistValidator()
    {
        RuleFor(x => x.NewPharmacyId)
            .NotEmpty().WithMessage("NewPharmacyId is required.");
    }
}