using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.AI.Services;

public class PromptExecutionService(IKernelFactory kernelFactory)
{
    public async Task<string> ExecutePromptAsync(
        string prompt,
        AIProvider provider = AIProvider.GitHubModels,
        ModelRole role = ModelRole.Chat)
    {
        var kernel = kernelFactory.GetKernel(provider, role);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await chatService.GetChatMessageContentAsync(history, kernel: kernel);
        return response.Content ?? string.Empty;
    }
}