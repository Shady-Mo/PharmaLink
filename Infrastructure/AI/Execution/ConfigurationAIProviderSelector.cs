using Application.Services.AI.Models;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;

namespace Infrastructure.AI.Execution;

public class ConfigurationAIProviderSelector(IOptions<AiOptions> options) : IAIProviderSelector
{
    private readonly AiOptions _options = options.Value;

    public AIProviderSelection Select(AITaskType taskType, string promptName)
    {
        var routeKey = $"{taskType}:{promptName}";
        if (!_options.TaskRouting.TryGetValue(routeKey, out var route))
        {
            _options.TaskRouting.TryGetValue(taskType.ToString(), out route);
        }

        var providerName = route?.Provider;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            providerName = taskType switch
            {
                AITaskType.Vision => _options.Defaults.VisionProvider,
                AITaskType.Embedding => _options.Defaults.EmbeddingProvider,
                _ => _options.Defaults.ChatProvider
            };
        }

        if (!Enum.TryParse<AIProvider>(providerName, ignoreCase: true, out var provider))
        {
            throw new InvalidOperationException($"AI provider '{providerName}' is not supported.");
        }

        return new AIProviderSelection
        {
            Provider = provider,
            Role = ToModelRole(taskType),
            ModelId = route?.ModelId
        };
    }

    private static ModelRole ToModelRole(AITaskType taskType)
    {
        return taskType switch
        {
            AITaskType.Vision => ModelRole.Vision,
            AITaskType.Embedding => ModelRole.Embedding,
            AITaskType.Rag => ModelRole.Chat,
            AITaskType.Agent => ModelRole.Reasoning,
            _ => ModelRole.Chat
        };
    }
}
