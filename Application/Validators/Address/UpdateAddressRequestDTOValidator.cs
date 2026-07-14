using Application.DTOs.Addresses.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators.Address
{

    public class UpdateAddressRequestDTOValidator : AbstractValidator<UpdateAddressRequestDTO>
    {
        public UpdateAddressRequestDTOValidator()
        {
            RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(500);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Governorate).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        }
    }
}
