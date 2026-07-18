namespace Application.Validators.Cart;

public class UpdateCartItemRequestDTOValidator : AbstractValidator<UpdateCartItemRequestDTO>
{
    public UpdateCartItemRequestDTOValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");
    }
}
