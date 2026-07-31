using Infrastructure.AI.Options;

namespace Infrastructure.AI.Prompts;

public class FileSystemPromptRegistry(IOptions<AiOptions> options) : IPromptRegistry
{
    private readonly PromptStoreOptions _options = options.Value.Prompts;

    public async Task<PromptDefinition> GetAsync(
        string promptName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        var promptVersion = string.IsNullOrWhiteSpace(version)
            ? _options.DefaultVersion
            : version;

        var baseDirectory = AppContext.BaseDirectory;
        var promptPath = Path.Combine(baseDirectory, _options.BasePath, $"{promptName}.{promptVersion}.prompty");

        if (!File.Exists(promptPath))
        {
            promptPath = Path.Combine(baseDirectory, _options.BasePath, $"{promptName}.prompty");
        }

        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Prompt '{promptName}' version '{promptVersion}' was not found.", promptPath);
        }

        return new PromptDefinition
        {
            Name = promptName,
            Version = promptVersion,
            Path = promptPath,
            Template = await File.ReadAllTextAsync(promptPath, cancellationToken)
        };
    }
}
