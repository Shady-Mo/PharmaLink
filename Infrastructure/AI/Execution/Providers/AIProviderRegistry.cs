namespace Infrastructure.AI.Execution.Providers;

public class AIProviderRegistry : IAIProviderRegistry
{
    private readonly Dictionary<string, IAIExecutionProvider> _providers;

    public AIProviderRegistry(IEnumerable<IAIExecutionProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
    }

    public IAIExecutionProvider GetProvider(string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            throw new InvalidOperationException($"AI Provider '{providerName}' is not registered.");
        }
        return provider;
    }

    public bool TryGetProvider(string providerName, out IAIExecutionProvider? provider)
    {
        return _providers.TryGetValue(providerName, out provider);
    }
}
