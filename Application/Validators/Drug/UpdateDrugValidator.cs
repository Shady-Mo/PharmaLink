namespace Application.Validators.Drug;

public class UpdateDrugValidator : AbstractValidator<UpdateDrugDto>
{
    public UpdateDrugValidator()
    {
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Manufacturer).MaximumLength(250);
        RuleFor(x => x.ArabicName).MaximumLength(250);
    }
}
