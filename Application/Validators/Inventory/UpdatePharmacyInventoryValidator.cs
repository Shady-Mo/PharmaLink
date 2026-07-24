using Application.DTOs.PharmacyInventory.Request;

namespace Application.Validators.Inventory;

public class UpdatePharmacyInventoryValidator : AbstractValidator<UpdatePharmacyInventoryDto>
{
    public UpdatePharmacyInventoryValidator()
    {
        RuleFor(i => i.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(i => i.UnitPrice).GreaterThan(0);
        RuleFor(i => i.ExpiryDate)
            .Must(d => d > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Expiry date must be in the future.");
    }
}
