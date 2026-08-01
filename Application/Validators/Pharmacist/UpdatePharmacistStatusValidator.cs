namespace Application.Validators.Pharmacist;

public class UpdatePharmacistStatusValidator : AbstractValidator<UpdatePharmacistStatusRequestDTO>
{
    public UpdatePharmacistStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid pharmacist status value.");
    }
}
