using Application.DTOs.Pharmacy.Request;
using FluentValidation;

namespace Application.Validators.Pharmacy
{
    public class GetAdminPharmaciesRequestValidator : AbstractValidator<GetAdminPharmaciesRequest>
    {
        public GetAdminPharmaciesRequestValidator()
        {
            RuleFor(p => p.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(p => p.PageSize).InclusiveBetween(1, 100);
            RuleFor(p => p.Status).IsInEnum().When(p => p.Status.HasValue);
        }
    }
}
