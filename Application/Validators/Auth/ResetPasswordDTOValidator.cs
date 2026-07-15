using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Auth
{
    public class ResetPasswordDTOValidator: AbstractValidator<ResetPasswordDTO>
    {
        public ResetPasswordDTOValidator()
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");

        }
    }
}
