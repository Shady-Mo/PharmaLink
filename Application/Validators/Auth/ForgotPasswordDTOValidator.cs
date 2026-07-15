using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Auth
{
    public class ForgotPasswordDTOValidator: AbstractValidator<ForgotPasswardDTO>
    {
        public ForgotPasswordDTOValidator()
        {
            RuleFor(f => f.Email).NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
        }
    }
}
