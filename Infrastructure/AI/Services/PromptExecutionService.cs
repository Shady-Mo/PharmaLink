using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.AI.Services;

public class PromptExecutionService
{
    private readonly IKernelFactory _kernelFactory;

    public PromptExecutionService(IKernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }

    public async Task<string> ExecutePromptAsync(
        string prompt, 
        AIProvider provider = AIProvider.GitHubModels, 
        ModelRole role = ModelRole.Chat)
    {
        var kernel = _kernelFactory.GetKernel(provider, role);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        
        var response = await chatService.GetChatMessageContentAsync(history, kernel: kernel);
        return response.Content ?? string.Empty;
    }
}
