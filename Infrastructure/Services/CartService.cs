namespace Infrastructure.Services;

public class CartService(AppDbContext dbContext, CartCacheService cartCache) : ICartService
{
    public async Task<Result<CartResponseDTO>> GetCartAsync(
        Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        var cached = await cartCache.GetAsync(patientUserId, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var cart = await FetchCartFromDbAsync(patientUserId, cancellationToken);

        if (cart is null)
        {
            var emptyResponse = new CartResponseDTO
            {
                CartId = Guid.Empty,
                PatientUserId = patientUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = []
            };
            return Result.Success(emptyResponse);
        }

        var response = cart.Adapt<CartResponseDTO>();
        await cartCache.SetAsync(patientUserId, response, cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<CartResponseDTO>> AddItemAsync(
        Guid patientUserId,
        AddCartItemRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var drug = await dbContext.Drugs
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DrugId == request.DrugId, cancellationToken);

        if (drug is null)
            return Result.Failure<CartResponseDTO>(CartErrors.DrugNotFound);

        var cart = await FetchCartFromDbAsync(patientUserId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart
            {
                PatientUserId = patientUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Carts.Add(cart);
        }

        var existingItem = cart.Items
            .FirstOrDefault(i => i.DrugId == request.DrugId);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var newItem = new CartItem
            {
                DrugId = request.DrugId,
                Quantity = request.Quantity,
                UnitPriceSnapshot = drug.Price
            };
            cart.Items.Add(newItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await cartCache.InvalidateAsync(patientUserId, cancellationToken);

        var response = cart.Adapt<CartResponseDTO>();
        return Result.Success(response);
    }

    public async Task<Result<CartResponseDTO>> UpdateItemAsync(
        Guid patientUserId,
        Guid cartItemId,
        UpdateCartItemRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var cart = await FetchCartFromDbAsync(patientUserId, cancellationToken);

        var item = cart?.Items.FirstOrDefault(i => i.CartItemId == cartItemId);

        if (cart is null || item is null)
            return Result.Failure<CartResponseDTO>(CartErrors.CartItemNotFound);

        item.Quantity = request.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await cartCache.InvalidateAsync(patientUserId, cancellationToken);

        var response = cart.Adapt<CartResponseDTO>();
        return Result.Success(response);
    }

    public async Task<Result> RemoveItemAsync(
        Guid patientUserId,
        Guid cartItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => 
                ci.CartItemId == cartItemId
                && ci.Cart.PatientUserId == patientUserId,
                cancellationToken);

        if (item is null)
            return Result.Failure(CartErrors.CartItemNotFound);

        item.Cart.UpdatedAt = DateTime.UtcNow;

        dbContext.CartItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cartCache.InvalidateAsync(patientUserId, cancellationToken);

        return Result.Success();
    }

    private async Task<Cart?> FetchCartFromDbAsync(Guid patientUserId, CancellationToken ct)
    {
        return await dbContext.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Drug)
            .FirstOrDefaultAsync(c => c.PatientUserId == patientUserId, ct);
    }
}
