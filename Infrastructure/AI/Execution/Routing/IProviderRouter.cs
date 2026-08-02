using Application.Services.AI.Models;

namespace Infrastructure.AI.Execution.Routing;

public interface IProviderRouter
{
    IEnumerable<ProviderModelTarget> GetTargets(AITaskType taskType, string promptName);
}
