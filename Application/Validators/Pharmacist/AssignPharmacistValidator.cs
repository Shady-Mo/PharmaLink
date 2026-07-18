namespace Application.Validators.Pharmacist;

public class AssignPharmacistValidator : AbstractValidator<AssignPharmacistRequestDTO>
{
    public AssignPharmacistValidator()
    {
        RuleFor(x => x.PharmacyId)
            .NotEmpty().WithMessage("PharmacyId is required.");
    }
}
