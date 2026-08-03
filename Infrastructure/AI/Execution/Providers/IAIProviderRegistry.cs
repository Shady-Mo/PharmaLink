namespace Infrastructure.AI.Execution.Providers;

/// <summary>
/// Registry for O(1) lookups of AI execution providers.
/// </summary>
public interface IAIProviderRegistry
{
    IAIExecutionProvider GetProvider(string providerName);
    bool TryGetProvider(string providerName, out IAIExecutionProvider? provider);
}
