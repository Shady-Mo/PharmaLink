using Infrastructure.AI.Models;

namespace Infrastructure.AI.Execution;

public sealed class AIProviderSelection
{
    public AIProvider Provider { get; init; }
    public ModelRole Role { get; init; }
    public string? ModelId { get; init; }
}
