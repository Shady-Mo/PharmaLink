namespace Application.Validators.Auth;

/// <summary>
/// Validator for <see cref="ChangePasswordRequestDTO"/>.
/// Enforces password policy and validates the request structure.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDTO>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.")
            .MinimumLength(8).WithMessage("Current password is invalid.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required.")
            .Equal(x => x.NewPassword).WithMessage("New passwords do not match.");

        RuleFor(x => x.NewPassword)
            .Must((request, newPassword) => newPassword != request.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }
}