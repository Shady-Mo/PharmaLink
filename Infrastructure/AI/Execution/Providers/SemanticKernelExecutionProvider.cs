using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Infrastructure.AI.Execution.Providers;

public class SemanticKernelExecutionProvider : IAIExecutionProvider
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IServiceProvider _serviceProvider;

    public SemanticKernelExecutionProvider(IKernelFactory kernelFactory, IServiceProvider serviceProvider)
    {
        _kernelFactory = kernelFactory;
        _serviceProvider = serviceProvider;
    }

    public string ProviderName => "SemanticKernel";

    public async Task<Result<PromptExecutionResult>> ExecuteAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AIProvider>(target.ProviderName, ignoreCase: true, out var providerEnum))
        {
            return Result.Failure<PromptExecutionResult>(new Error("ProviderError",
                "Unsupported Semantic Kernel Provider: {target.ProviderName}", 500));
        }

        var role = request.TaskType switch
        {
            AITaskType.Vision => ModelRole.Vision,
            AITaskType.Embedding => ModelRole.Embedding,
            AITaskType.Agent => ModelRole.Reasoning,
            _ => ModelRole.Chat
        };

        var kernel = _kernelFactory.GetKernel(providerEnum, role, target.ModelId);
        
        kernel.AddPharmacyPlugins(_serviceProvider);
        
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = BuildChatHistory(request, renderedPrompt);
        
        var settings = BuildExecutionSettings(providerEnum);

        var response = await chatService.GetChatMessageContentAsync(
            history,
            executionSettings: settings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        return Result<PromptExecutionResult>.Success(new PromptExecutionResult
        {
            PromptName = prompt.Name,
            PromptVersion = prompt.Version,
            Provider = target.ProviderName,
            ModelId = target.ModelId,
            RawResponse = response.Content ?? string.Empty
        });
    }

    public async IAsyncEnumerable<string> ExecuteStreamAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AIProvider>(target.ProviderName, ignoreCase: true, out var providerEnum))
        {
            throw new InvalidOperationException($"Unsupported Semantic Kernel Provider: {target.ProviderName}");
        }

        var role = request.TaskType switch
        {
            AITaskType.Vision => ModelRole.Vision,
            AITaskType.Embedding => ModelRole.Embedding,
            AITaskType.Agent => ModelRole.Reasoning,
            _ => ModelRole.Chat
        };

        var kernel = _kernelFactory.GetKernel(providerEnum, role, target.ModelId);
        
        kernel.AddPharmacyPlugins(_serviceProvider);
        
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = BuildChatHistory(request, renderedPrompt);
        var settings = BuildExecutionSettings(providerEnum);

        var stream = chatService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings: settings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }

    private static ChatHistory BuildChatHistory(PromptExecutionRequest request, string renderedPrompt)
    {
        var history = new ChatHistory();

        if (request.ChatHistory != null)
        {
            // For chat, renderedPrompt is the system prompt.
            history = new ChatHistory(renderedPrompt);

            foreach (var msg in request.ChatHistory)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    history.AddUserMessage(msg.Content);
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    history.AddAssistantMessage(msg.Content);
            }

            if (!string.IsNullOrWhiteSpace(request.UserMessage))
            {
                if (history.Count == 0 || history.Last().Content != request.UserMessage)
                {
                    history.AddUserMessage(request.UserMessage);
                }
            }

            // Fix for Gemini: collapse adjacent messages of the same role
            var collapsedHistory = new ChatHistory(renderedPrompt);
            foreach (var msg in history.Where(m => m.Role != AuthorRole.System))
            {
                if (collapsedHistory.Count > 0 && collapsedHistory.Last().Role == msg.Role)
                {
                    collapsedHistory.Last().Content += "\n\n" + msg.Content;
                }
                else
                {
                    if (msg.Role == AuthorRole.User) collapsedHistory.AddUserMessage(msg.Content ?? string.Empty);
                    else if (msg.Role == AuthorRole.Assistant) collapsedHistory.AddAssistantMessage(msg.Content ?? string.Empty);
                }
            }
            return collapsedHistory;
        }
        else
        {
            if (request.File is null)
            {
                history.AddUserMessage(renderedPrompt);
            }
            else
            {
                var items = new ChatMessageContentItemCollection
                {
                    new TextContent(renderedPrompt),
                    CreateFileContent(request.File)
                };

                history.AddUserMessage(items);
            }
        }

        return history;
    }

    private static PromptExecutionSettings BuildExecutionSettings(AIProvider provider)
    {
        if (provider == AIProvider.Gemini)
        {
            return new GeminiPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions { AllowConcurrentInvocation = false })
            };
        }

        return new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions { AllowConcurrentInvocation = false })
        };
    }

    private static KernelContent CreateFileContent(AIFileContent file)
    {
#pragma warning disable SKEXP0001
        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return new ImageContent(file.Content, file.ContentType);
        }

        return new BinaryContent(file.Content, file.ContentType);
#pragma warning restore SKEXP0001
    }
}

