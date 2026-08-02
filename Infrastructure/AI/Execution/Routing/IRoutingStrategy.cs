using Infrastructure.AI.Options;

namespace Infrastructure.AI.Execution.Routing;

public interface IRoutingStrategy
{
    string Name { get; }
    IEnumerable<ProviderModelTarget> GetTargets(TaskRouteConfig config);
}
