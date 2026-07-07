using FluentValidation;
using Application.DTOs.Drug.Requests;

namespace Application.Validators.Drug;

public class UpdateDrugValidator : AbstractValidator<UpdateDrugDto>
{
    public UpdateDrugValidator()
    {
        RuleFor(x => x.GenericName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.NdcCode).MaximumLength(50);
    }
}
