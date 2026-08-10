using Application.Common;
using Application.Services.AI.Models;
using Infrastructure.AI.Prompts;
using Infrastructure.AI.Execution.Routing;

namespace Infrastructure.AI.Execution.Providers;

/// <summary>
/// Defines a provider that can execute AI prompts. 
/// Examples: GeminiExecutionProvider, SemanticKernelExecutionProvider
/// </summary>
public interface IAIExecutionProvider
{
    string ProviderName { get; }

    Task<Result<PromptExecutionResult>> ExecuteAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        CancellationToken cancellationToken);

    IAsyncEnumerable<string> ExecuteStreamAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        CancellationToken cancellationToken);
}
