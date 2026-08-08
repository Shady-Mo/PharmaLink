using Microsoft.SemanticKernel;

namespace Infrastructure.Services.PrescriptionAudit;

public class CartBuilderPlugin(AppDbContext context, CartCacheService cartCache) : ICartBuilderPlugin
{
    [KernelFunction("create_prescription_cart")]
    public async Task<CartBuildResult> CreateCartAsync(
        Guid patientUserId,
        Guid prescriptionReviewId,
        IReadOnlyCollection<PrescriptionReviewMedicine> medicines,
        CancellationToken cancellationToken = default)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.PatientUserId == patientUserId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart
            {
                CartId = Guid.NewGuid(),
                PatientUserId = patientUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Carts.Add(cart);
        }

        foreach (var medicine in medicines)
        {
            if (medicine.MatchStatus is PrescriptionMedicineMatchStatus.ExactMatch or PrescriptionMedicineMatchStatus.FuzzyMatch
                && medicine.MatchedDrugId.HasValue)
            {
                await UpsertCartItemAsync(
                    cart,
                    medicine.MatchedDrugId.Value,
                    medicine,
                    cancellationToken);
            }

            if (medicine.MatchStatus == PrescriptionMedicineMatchStatus.AlternativeSuggested
                && medicine.SuggestedAlternativeDrugId.HasValue)
            {
                await UpsertCartItemAsync(
                    cart,
                    medicine.SuggestedAlternativeDrugId.Value,
                    medicine,
                    cancellationToken);
            }

        }

        cart.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await cartCache.InvalidateAsync(patientUserId, cancellationToken);

        return new CartBuildResult { CartId = cart.CartId };
    }

    private async Task UpsertCartItemAsync(
        Cart cart,
        Guid drugId,
        PrescriptionReviewMedicine medicine,
        CancellationToken cancellationToken)
    {
        var drug = await context.Drugs
            .AsNoTracking()
            .FirstAsync(d => d.DrugId == drugId, cancellationToken);

        var quantity = Math.Max(medicine.Quantity, 1);
        var existingItem = cart.Items.FirstOrDefault(i => i.DrugId == drugId);

        if (existingItem is not null)
        {
            existingItem.Quantity += quantity;
            existingItem.UnitPriceSnapshot = drug.Price;
            return;
        }

        context.CartItems.Add(new CartItem
        {
            CartItemId = Guid.NewGuid(),
            CartId = cart.CartId,
            DrugId = drugId,
            Quantity = quantity,
            UnitPriceSnapshot = drug.Price
        });
    }
}
