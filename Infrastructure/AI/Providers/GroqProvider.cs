using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Providers;

public class GroqProvider : IKernelProvider
{
    private readonly GroqOptions _options;
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();

    public GroqProvider(IOptions<AiOptions> options)
    {
        _options = options.Value.Providers.Groq;
    }

    public AIProvider Provider => AIProvider.Groq;

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
            throw new InvalidOperationException($"Model {selectedModelId} is not configured for role {roleName} in {Provider}.");
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
                $"Missing Groq API key. Set environment variable '{_options.ApiKeyEnvironmentVariable}'.");
        }

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri(_options.BaseUrl)
        );
        return builder.Build();
    }
}
