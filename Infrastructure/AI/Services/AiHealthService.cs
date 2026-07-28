using Infrastructure.AI.Models;

namespace Infrastructure.AI.Services;

public class AiHealthService(PromptExecutionService promptExecutionService, EmbeddingService embeddingService)
{
    public async Task<Dictionary<string, string>> CheckHealthAsync()
    {
        var results = new Dictionary<string, string>();

        try
        {
            await promptExecutionService.ExecutePromptAsync("Say OK", AIProvider.Groq, ModelRole.Chat);
            results.Add("Groq_Chat", "Healthy");
        }
        catch (Exception ex)
        {
            results.Add("Groq_Chat", $"Unhealthy: {ex.Message}");
        }

        try
        {
            await promptExecutionService.ExecutePromptAsync("Say OK", AIProvider.Gemini, ModelRole.Chat);
            results.Add("Gemini_Chat", "Healthy");
        }
        catch (Exception ex)
        {
            results.Add("Gemini_Chat", $"Unhealthy: {ex.Message}");
        }

        try
        {
            await promptExecutionService.ExecutePromptAsync("Say OK", AIProvider.GitHubModels, ModelRole.Chat);
            results.Add("GitHubModels_Chat", "Healthy");
        }
        catch (Exception ex)
        {
            results.Add("GitHubModels_Chat", $"Unhealthy: {ex.Message}");
        }

        try
        {
            await embeddingService.GenerateEmbeddingAsync("Test", AIProvider.GitHubModels);
            results.Add("GitHubModels_Embedding", "Healthy");
        }
        catch (Exception ex)
        {
            results.Add("GitHubModels_Embedding", $"Unhealthy: {ex.Message}");
        }

        return results;
    }
}