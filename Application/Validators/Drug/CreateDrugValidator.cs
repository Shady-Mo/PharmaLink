namespace Application.Validators.Drug;

public class CreateDrugValidator : AbstractValidator<CreateDrugDto>
{
    public CreateDrugValidator()
    {
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Manufacturer).MaximumLength(250);
        RuleFor(x => x.ArabicName).MaximumLength(250);
    }
}