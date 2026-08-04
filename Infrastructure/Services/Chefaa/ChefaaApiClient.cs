using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Services.Chefaa;

public class ChefaaApiClient : IChefaaApiClient
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "aa66bf66db30dea9b9746c8f6397d7a0112a055c70d80527b300c3dec85fcc41";
    private const string SearchUrl = "https://meilisearch.chefaa.com/indexes/products_eg/search";

    public ChefaaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    }

    public async Task<JsonObject> FetchProductsAsync(int limit, decimal? lastPrice = null, CancellationToken cancellationToken = default)
    {
        var requestBody = new JsonObject
        {
            ["q"] = "",
            ["limit"] = limit,
            ["sort"] = new JsonArray { "price:asc" }
        };

        if (lastPrice.HasValue)
        {
            requestBody["filter"] = new JsonArray { $"price >= {lastPrice.Value}" };
        }

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<System.IO.IOException>()
            .Or<System.Net.Sockets.SocketException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(7, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[ChefaaApiClient] HTTP request failed (Attempt {retryCount}). Retrying in {timeSpan.TotalSeconds}s... Error: {exception.Message}");
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(SearchUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonNode = JsonNode.Parse(jsonString);

            return jsonNode?.AsObject() ?? new JsonObject();
        });
    }
}
