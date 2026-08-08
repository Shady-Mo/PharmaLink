namespace Application.Validators.Pharmacist;

public class AssignPharmacistToBranchValidator : AbstractValidator<AssignPharmacistToBranchRequestDTO>
{
    public AssignPharmacistToBranchValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("Branch ID is required.");
    }
}
