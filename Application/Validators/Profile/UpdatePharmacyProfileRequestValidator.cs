namespace Application.Validators.Profile
{
    public class UpdatePharmacistProfileRequestValidator: AbstractValidator<UpdatePharmacistProfileRequestDTO>
    {
        public UpdatePharmacistProfileRequestValidator()
        {
            RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");


            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^(?:\+20|0020|0)?1[0125][0-9]{8}$").WithMessage("Invalid Egyptian phone number");
        }
    }
}
