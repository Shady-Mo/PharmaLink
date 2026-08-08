using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Providers;

public class GeminiProvider(IOptions<AiOptions> options) : IKernelProvider
{
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();
    private readonly GeminiOptions _options = options.Value.Providers.Gemini;

    public AIProvider Provider => AIProvider.Gemini;

    public Kernel GetKernel(ModelRole role, string? modelId = null)
    {
        var roleName = role.ToString();
        if (!_options.Models.TryGetValue(roleName, out var configuredModels) || configuredModels.Length == 0)
        {
            throw new InvalidOperationException($"Model for role {roleName} is not configured for {Provider}.");
        }

        var selectedModelId = modelId ?? configuredModels[0];

        if (!configuredModels.Contains(selectedModelId))
        {
            throw new InvalidOperationException(
                $"Model {selectedModelId} is not configured for role {roleName} in {Provider}.");
        }

        return _kernels.GetOrAdd(selectedModelId, CreateKernel);
    }

    private Kernel CreateKernel(string modelId)
    {
#pragma warning disable SKEXP0070
        var apiKey = ResolveApiKey();

        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIGeminiChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            apiVersion: GoogleAIVersion.V1_Beta
        );
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
                $"Missing Gemini API key. Set environment variable '{_options.ApiKeyEnvironmentVariable}'.");
        }

        return apiKey;
    }
}
