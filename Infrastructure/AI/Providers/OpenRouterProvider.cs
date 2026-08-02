using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Providers;

/// <summary>
/// Kernel provider for OpenRouter — a unified API gateway exposing 400+ models
/// (Claude, Gemini, GPT, DeepSeek, Grok, Kimi, etc.) via a single OpenAI-compatible endpoint.
///
/// DESIGN DECISION — Why OpenRouter?
///   • Single API key for access to every major model family.
///   • Automatic failover: if one upstream provider is down, OpenRouter retries others.
///   • Cost transparency: pricing per token visible at openrouter.ai/models.
///   • The API is 100% OpenAI-compatible, so we reuse AddOpenAIChatCompletion
///     with a custom base endpoint — no additional NuGet packages required.
///
/// BASE URL  : https://openrouter.ai/api/v1
/// AUTH      : Authorization: Bearer {ApiKey}
/// OPT HDRS  : HTTP-Referer (site URL) and X-Title (app name) for analytics.
/// </summary>
public class OpenRouterProvider(IOptions<AiOptions> options) : IKernelProvider
{
    private readonly OpenRouterOptions _options = options.Value.Providers.OpenRouter;
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();

    public AIProvider Provider => AIProvider.OpenRouter;

    /// <inheritdoc />
    public Kernel GetKernel(ModelRole role, string? modelId = null)
    {
        var roleName = role.ToString();

        if (!_options.Models.TryGetValue(roleName, out var configuredModels) || configuredModels.Length == 0)
        {
            throw new InvalidOperationException(
                $"No models configured for role '{roleName}' under AI:Providers:OpenRouter:Models. " +
                $"Add at least one model ID from https://openrouter.ai/models.");
        }

        var selectedModelId = modelId ?? configuredModels[0];

        if (!configuredModels.Contains(selectedModelId))
        {
            throw new InvalidOperationException(
                $"Model '{selectedModelId}' is not in the allowed list for role '{roleName}' " +
                $"in OpenRouter. Allowed: [{string.Join(", ", configuredModels)}].");
        }

        // Cache by model ID so each model gets exactly one Kernel (thread-safe).
        return _kernels.GetOrAdd(selectedModelId, CreateKernel);
    }

    // ---------------------------------------------------------------------------
    //  Private helpers
    // ---------------------------------------------------------------------------

    private Kernel CreateKernel(string modelId)
    {
        var apiKey = ResolveApiKey();

        // Build an HttpClient that injects the optional OpenRouter headers.
        // These headers are not required for functionality but enable app
        // attribution in the OpenRouter dashboard and rankings.
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(_options.SiteUrl))
            httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _options.SiteUrl);

        if (!string.IsNullOrWhiteSpace(_options.SiteName))
            httpClient.DefaultRequestHeaders.Add("X-Title", _options.SiteName);

        var builder = Kernel.CreateBuilder();

        // OpenRouter is 100% OpenAI-compatible.
        // We pass the custom HttpClient so the referer/title headers are forwarded.
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            httpClient: httpClient);

        return builder.Build();
    }

    private string ResolveApiKey()
    {
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable)
            : _options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Missing OpenRouter API key. " +
                $"Set the environment variable '{_options.ApiKeyEnvironmentVariable}' " +
                $"or populate 'AI:Providers:OpenRouter:ApiKey' in appsettings.");
        }

        return apiKey;
    }
}