using Application.DTOs.PharmacyBranch.Request;

namespace Application.Validators.PharmacyBranch;

public class CreatePharmacyBranchValidator : AbstractValidator<CreatePharmacyBranchDTO>
{
    public CreatePharmacyBranchValidator()
    {
        RuleFor(b => b.BranchName).NotEmpty().MaximumLength(150);
        RuleFor(b => b.City).NotEmpty().MaximumLength(100);
        RuleFor(b => b.Governorate).NotEmpty().MaximumLength(100);
        RuleFor(b => b.AddressLine).NotEmpty().MaximumLength(250);
        RuleFor(b => b.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(b => b.WorkingHours).NotEmpty().MaximumLength(150);

        RuleFor(b => b.ServiceRadiusKm).GreaterThanOrEqualTo(0);

        // Coordinates are optional, but if either is supplied both are required and must be in range.
        When(b => b.Latitude.HasValue || b.Longitude.HasValue, () =>
        {
            RuleFor(b => b.Latitude)
                .NotNull().WithMessage("Latitude is required when Longitude is provided.")
                .InclusiveBetween(-90, 90);

            RuleFor(b => b.Longitude)
                .NotNull().WithMessage("Longitude is required when Latitude is provided.")
                .InclusiveBetween(-180, 180);
        });
    }
}
