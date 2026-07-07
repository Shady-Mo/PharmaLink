using FluentValidation;
using Application.DTOs.Drug.Requests;

namespace Application.Validators.Drug;

public class CreateDrugValidator : AbstractValidator<CreateDrugDto>
{
    public CreateDrugValidator()
    {
        RuleFor(x => x.GenericName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.NdcCode).MaximumLength(50);
    }
}