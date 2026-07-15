using Application.DTOs.Pharmacy.Request;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Profile
{
    public class UpdatePharmacyProfileRequestValidator: AbstractValidator<UpdatePharmacyProfileRequestDTO>
    {
        public UpdatePharmacyProfileRequestValidator()
        {
            RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");


            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^(?:\+20|0020|0)?1[0125][0-9]{8}$").WithMessage("Invalid Egyptian phone number");
        }
    }
}
