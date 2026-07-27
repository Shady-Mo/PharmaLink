using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Factories;

public class KernelFactory : IKernelFactory
{
    private readonly Dictionary<AIProvider, IKernelProvider> _providers;

    public KernelFactory(IEnumerable<IKernelProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Provider);
    }

    public Kernel GetKernel(AIProvider provider, ModelRole role, string? modelId = null)
    {
        if (!_providers.TryGetValue(provider, out var kernelProvider))
        {
            throw new InvalidOperationException($"Provider {provider} is not registered.");
        }

        return kernelProvider.GetKernel(role, modelId);
    }
}
