namespace Application.Validators.Cart;

public class AddCartItemRequestDTOValidator : AbstractValidator<AddCartItemRequestDTO>
{
    public AddCartItemRequestDTOValidator()
    {
        RuleFor(x => x.DrugId)
            .NotEmpty()
            .WithMessage("DrugId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");
    }
}
