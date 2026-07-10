namespace Application.Validators.Auth;

public class OtpRequestValidator : AbstractValidator<OtpRequestDTO>
{
    public OtpRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
