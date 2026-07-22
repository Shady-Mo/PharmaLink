using Application.DTOs.PharmacyOwner.Request;
using FluentValidation;

namespace Application.Validators.PharmacyOwner
{
    public class CreatePharmacyOwnerDTOValidator : AbstractValidator<CreatePharmacyOwnerDTO>
    {
        public CreatePharmacyOwnerDTOValidator()
        {
            RuleFor(a => a.FullName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(150);

            RuleFor(a => a.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(a => a.PhoneNumber)
                .NotEmpty()
                .Matches(@"^\+?[0-9]{10,15}$")
                .WithMessage("Phone number must be a valid international format (10-15 digits).");

            RuleFor(a => a.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(100)
                .Matches("[A-Z]").WithMessage("Passwords must have at least one uppercase ('A'-'Z').")
                .Matches("[0-9]").WithMessage("Passwords must have at least one digit ('0'-'9').")
                .Matches("[^a-zA-Z0-9]").WithMessage("Passwords must have at least one non alphanumeric character.");

            RuleFor(a => a.PharmacyId)
                .NotEmpty()
                .WithMessage("Pharmacy ID is required.");
        }
    }
}
