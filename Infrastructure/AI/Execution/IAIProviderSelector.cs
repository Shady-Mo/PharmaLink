using Application.Services.AI.Models;

namespace Infrastructure.AI.Execution;

public interface IAIProviderSelector
{
    AIProviderSelection Select(AITaskType taskType, string promptName);
}
