using System.Text.Json.Nodes;

namespace Infrastructure.Services.Chefaa;

public interface IChefaaApiClient
{
    Task<JsonObject> FetchProductsAsync(int limit, decimal? lastPrice = null, CancellationToken cancellationToken = default);
}
