namespace Infrastructure.AI.Prompts;

public sealed class PromptDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = "v1";
    public string Path { get; init; } = string.Empty;
    public string Template { get; init; } = string.Empty;
}
