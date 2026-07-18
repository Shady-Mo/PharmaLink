namespace Application.Services.Cart;

public interface ICartService
{
    /// <summary>
    /// Returns the patient's cart from Redis cache (cache-aside).
    /// On cache miss, queries the DB and repopulates the cache.
    /// </summary>
    Task<Result<CartResponseDTO>> GetCartAsync(
        Guid patientUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a drug to the cart (auto-creates the cart if it doesn't exist).
    /// Upserts if the drug is already present. Invalidates the Redis cache.
    /// </summary>
    Task<Result<CartResponseDTO>> AddItemAsync(
        Guid patientUserId,
        AddCartItemRequestDTO request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the quantity of an existing cart item.
    /// Invalidates the Redis cache.
    /// </summary>
    Task<Result<CartResponseDTO>> UpdateItemAsync(
        Guid patientUserId,
        Guid cartItemId,
        UpdateCartItemRequestDTO request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific item from the cart.
    /// Invalidates the Redis cache.
    /// </summary>
    Task<Result> RemoveItemAsync(
        Guid patientUserId,
        Guid cartItemId,
        CancellationToken cancellationToken = default);
}
