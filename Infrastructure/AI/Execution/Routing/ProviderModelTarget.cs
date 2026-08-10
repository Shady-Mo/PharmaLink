namespace Infrastructure.AI.Execution.Routing;

public record ProviderModelTarget(
    string ProviderName,
    string ModelId,
    int ProviderPriority,
    int ModelPriority,
    int ProviderWeight,
    int ModelWeight,
    int TimeoutSeconds
);
