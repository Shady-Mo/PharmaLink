using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Providers;

public class GitHubModelsProvider(IOptions<AiOptions> options) : IKernelProvider
{
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();
    private readonly GitHubModelsOptions _options = options.Value.Providers.GitHubModels;

    public AIProvider Provider => AIProvider.GitHubModels;

    public Kernel GetKernel(ModelRole role, string? modelId = null)
    {
        var roleName = role.ToString();
        if (!_options.Models.TryGetValue(roleName, out var configuredModels) || configuredModels.Length == 0)
        {
            throw new InvalidOperationException($"Model for role {roleName} is not configured for {Provider}.");
        }

        var selectedModelId = modelId ?? configuredModels[0];

        return !configuredModels.Contains(selectedModelId)
            ? throw new InvalidOperationException(
                $"Model {selectedModelId} is not configured for role {roleName} in {Provider}.")
            : _kernels.GetOrAdd($"{roleName}_{selectedModelId}", _ => CreateKernel(selectedModelId, role));
    }

    private Kernel CreateKernel(string modelId, ModelRole role)
    {
#pragma warning disable SKEXP0070
        var builder = Kernel.CreateBuilder();

        if (role == ModelRole.Embedding)
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(_options.EmbeddingEndpoint) };
            builder.AddOpenAITextEmbeddingGeneration(
                modelId: modelId,
                apiKey: _options.Token,
                httpClient: httpClient
            );
        }
        else
        {
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: _options.Token,
                endpoint: new Uri(_options.BaseUrl)
            );
        }

        return builder.Build();
    }
}