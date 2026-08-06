using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Infrastructure.AI.Providers;

public sealed class ITIOrderSplittingProvider(IOptions<AiOptions> options) : IKernelProvider
{
    private readonly ITIOptions _options = options.Value.Providers.ITIOrderSplitting;
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();

    public AIProvider Provider => AIProvider.ITIOrderSplitting;

    public Kernel GetKernel(ModelRole role, string? modelId = null)
    {
        var roleName = role.ToString();
        if (!_options.Models.TryGetValue(roleName, out var configuredModels) || configuredModels.Length == 0)
        {
            throw new InvalidOperationException(
                $"No models configured for role '{roleName}' under AI:Providers:ITIOrderSplitting:Models. " +
                $"Add at least one model ID (e.g., 'anthropic.[REDACTED]v1:0').");
        }

        var selectedModelId = modelId ?? configuredModels[0];

        if (!configuredModels.Contains(selectedModelId))
        {
            throw new InvalidOperationException(
                $"Model '{selectedModelId}' is not in the allowed list for role '{roleName}' " +
                $"in ITIOrderSplitting. Allowed: [{string.Join(", ", configuredModels)}].");
        }

        return _kernels.GetOrAdd(selectedModelId, CreateKernel);
    }

    private Kernel CreateKernel(string modelId)
    {
#pragma warning disable SKEXP0070
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable)
            : _options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Missing ITI API key for ITIOrderSplitting. " +
                $"Set environment variable '{_options.ApiKeyEnvironmentVariable}' or populate " +
                $"'AI:Providers:ITIOrderSplitting:ApiKey' in appsettings.json.");
        }

        var builder = Kernel.CreateBuilder();

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            apiVersion: GoogleAIVersion.V1_Beta
        );

        return builder.Build();
#pragma warning restore SKEXP0070
    }
}
