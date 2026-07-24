using Application.DTOs.PharmacyInventory.Request;

namespace Application.Validators.Inventory;

public class AddPharmacyInventoryValidator : AbstractValidator<AddPharmacyInventoryDto>
{
    public AddPharmacyInventoryValidator()
    {
        RuleFor(i => i.BranchId).NotEmpty();
        RuleFor(i => i.DrugId).NotEmpty();
        RuleFor(i => i.StockQuantity).GreaterThan(0);
        RuleFor(i => i.UnitPrice).GreaterThan(0);
        RuleFor(i => i.ExpiryDate)
            .Must(d => d > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Expiry date must be in the future.");
    }
}
