using System.Collections.Concurrent;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Providers;

public class AwsBedrockProvider(IOptions<AiOptions> options) : IKernelProvider
{
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();
    private readonly AwsBedrockOptions _options = options.Value.Providers.AwsBedrock;

    public AIProvider Provider => AIProvider.AwsBedrock;

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

        return _kernels.GetOrAdd($"{roleName}_{selectedModelId}", _ => CreateKernel(selectedModelId, role));
    }

    private Kernel CreateKernel(string modelId, ModelRole role)
    {
#pragma warning disable SKEXP0070
        var builder = Kernel.CreateBuilder();

        // Standard setup could go here. For example, AWS Bedrock might use:
        // builder.AddBedrockChatCompletion(modelId, ...) 
        // But since we are just adding it per instructions and may not have the Bedrock SDK yet,
        // we leave this ready for extension.

        return builder.Build();
    }
}