using Application.Services.AI.Models;

namespace Application.Services.AI;

public interface IPromptExecutionService
{
    Task<PromptExecutionResult> ExecuteAsync(
        PromptExecutionRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ExecuteStreamAsync(
        PromptExecutionRequest request,
        CancellationToken cancellationToken = default);
}
