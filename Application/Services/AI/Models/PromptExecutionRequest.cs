namespace Application.Services.AI.Models;

public sealed class PromptExecutionRequest
{
    public string PromptName { get; init; } = string.Empty;
    public string PromptVersion { get; init; } = "v1";
    public AITaskType TaskType { get; init; } = AITaskType.Chat;
    public AIFileContent? File { get; init; }
    public Dictionary<string, object?> Variables { get; init; } = new();
    public IReadOnlyList<ChatMessage>? ChatHistory { get; init; }
    public string? UserMessage { get; init; }
}
