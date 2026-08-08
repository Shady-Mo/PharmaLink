using Infrastructure.AI.Options;

namespace Infrastructure.AI.Execution.Routing;

public class PriorityRoutingStrategy : IRoutingStrategy
{
    public string Name => "Priority";

    public IEnumerable<ProviderModelTarget> GetTargets(TaskRouteConfig config)
    {
        var targets = new List<ProviderModelTarget>();

        var sortedProviders = config.Providers
            .Where(p => p.Value.Enabled)
            .OrderBy(p => p.Value.Priority)
            .ToList();

        foreach (var (providerName, providerConfig) in sortedProviders)
        {
            var sortedModels = providerConfig.Models
                .Where(m => m.Enabled)
                .OrderBy(m => m.Priority)
                .ToList();

            foreach (var model in sortedModels)
            {
                targets.Add(new ProviderModelTarget(
                    providerName,
                    model.ModelId,
                    providerConfig.Priority,
                    model.Priority,
                    providerConfig.Weight,
                    model.Weight,
                    model.TimeoutSeconds
                ));
            }
        }

        return targets;
    }
}