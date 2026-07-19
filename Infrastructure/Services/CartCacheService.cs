namespace Infrastructure.Services;

public class CartCacheService(IDistributedCache cache)
{
    private static string BuildKey(Guid patientUserId) => $"cart:{patientUserId}";

    private static readonly DistributedCacheEntryOptions DefaultOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    public async Task<CartResponseDTO?> GetAsync(Guid patientUserId, CancellationToken ct = default)
    {
        var cart = await cache.GetStringAsync(BuildKey(patientUserId), ct);

        return cart is null
            ? null
            : JsonSerializer.Deserialize<CartResponseDTO>(cart);
    }

    public async Task SetAsync(Guid patientUserId, CartResponseDTO CartResponseDTO, CancellationToken ct = default)
    {
        var cart = JsonSerializer.Serialize(CartResponseDTO);
        await cache.SetStringAsync(BuildKey(patientUserId), cart, DefaultOptions, ct);
    }

    public async Task InvalidateAsync(Guid patientUserId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(BuildKey(patientUserId), ct);
    }
}
