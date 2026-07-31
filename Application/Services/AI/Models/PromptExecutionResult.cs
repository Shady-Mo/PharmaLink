namespace Application.Services.AI.Models;

public sealed class PromptExecutionResult
{
    public string PromptName { get; init; } = string.Empty;
    public string PromptVersion { get; init; } = "v1";
    public string Provider { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public string RawResponse { get; init; } = string.Empty;
    public long LatencyMs { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
}
