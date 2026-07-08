using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Auth
{
    public class LoginRequestDTOValidator:AbstractValidator<LoginRequestDTO>
    {
        public LoginRequestDTOValidator()
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
