using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Providers;

public class GeminiProvider : IKernelProvider
{
    private readonly GeminiOptions _options;
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();

    public GeminiProvider(IOptions<AiOptions> options)
    {
        _options = options.Value.Providers.Gemini;
    }

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
            throw new InvalidOperationException($"Model {selectedModelId} is not configured for role {roleName} in {Provider}.");
        }

        return _kernels.GetOrAdd(selectedModelId, CreateKernel);
    }

    private Kernel CreateKernel(string modelId)
    {
#pragma warning disable SKEXP0070
        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIGeminiChatCompletion(
            modelId: modelId,
            apiKey: _options.ApiKey
        );
        return builder.Build();
    }
}
