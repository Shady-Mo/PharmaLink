using Application.DTOs.Addresses.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Address
{

    public class CreateAddressRequestDTOValidator : AbstractValidator<CreateAddressRequestDTO>
    {
        public CreateAddressRequestDTOValidator()
        {

            RuleFor(x => x.AddressLine)
                .NotEmpty().WithMessage("Address line is required.")
                .MaximumLength(500);

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100);

            RuleFor(x => x.Governorate)
                .NotEmpty().WithMessage("Governorate is required.")
                .MaximumLength(100);

            // AC: invalid lat/long values (outside valid geographic range) -> 400
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
        }
    }
}
