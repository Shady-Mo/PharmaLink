using Application.Services.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.AI.Execution.Routing;

public class ProviderRouter : IProviderRouter
{
    private readonly AiOptions _options;
    private readonly IEnumerable<IRoutingStrategy> _strategies;

    public ProviderRouter(IOptions<AiOptions> options, IEnumerable<IRoutingStrategy> strategies)
    {
        _options = options.Value;
        _strategies = strategies;
    }

    public IEnumerable<ProviderModelTarget> GetTargets(AITaskType taskType, string promptName)
    {
        var routeKey = $"{taskType}:{promptName}";
        if (!_options.TaskRouting.TryGetValue(routeKey, out var config))
        {
            if (!_options.TaskRouting.TryGetValue(taskType.ToString(), out config))
            {
                config = CreateDefaultConfig(taskType);
            }
        }

        var strategyName = config.RoutingStrategy ?? "Priority";
        var strategy = _strategies.FirstOrDefault(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
        
        if (strategy == null)
        {
            throw new InvalidOperationException($"Routing strategy '{strategyName}' is not registered.");
        }

        return strategy.GetTargets(config);
    }

    private TaskRouteConfig CreateDefaultConfig(AITaskType taskType)
    {
        var providerName = taskType switch
        {
            AITaskType.Vision => _options.Defaults.VisionProvider,
            AITaskType.Embedding => _options.Defaults.EmbeddingProvider,
            _ => _options.Defaults.ChatProvider
        };

        if (string.IsNullOrWhiteSpace(providerName))
        {
            providerName = _options.Defaults.Provider ?? "Gemini";
        }

        var providerConfig = new ProviderConfig
        {
            Enabled = true,
            Priority = 1,
            Models = new List<ModelConfig>
            {
                new ModelConfig { ModelId = "", Priority = 1, Enabled = true }
            }
        };

        return new TaskRouteConfig
        {
            RoutingStrategy = "Priority",
            Providers = new Dictionary<string, ProviderConfig>(StringComparer.OrdinalIgnoreCase)
            {
                { providerName, providerConfig }
            }
        };
    }
}
