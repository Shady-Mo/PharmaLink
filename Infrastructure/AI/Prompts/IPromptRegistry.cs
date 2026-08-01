namespace Infrastructure.AI.Prompts;

public interface IPromptRegistry
{
    Task<PromptDefinition> GetAsync(
        string promptName,
        string? version = null,
        CancellationToken cancellationToken = default);
}
