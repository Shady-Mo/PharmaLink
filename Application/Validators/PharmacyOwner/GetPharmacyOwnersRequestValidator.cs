using Application.DTOs.PharmacyOwner.Request;
using FluentValidation;

namespace Application.Validators.PharmacyOwner
{
    public class GetPharmacyOwnersRequestValidator : AbstractValidator<GetPharmacyOwnersRequest>
    {
        public GetPharmacyOwnersRequestValidator()
        {
            RuleFor(a => a.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(a => a.PageSize).InclusiveBetween(1, 100);
            RuleFor(a => a.Status).IsInEnum().When(a => a.Status.HasValue);
        }
    }
}
