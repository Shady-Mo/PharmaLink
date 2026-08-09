using System.Text;
using System.Text.Json;
using Application.Services.AI.Models;
using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.AI.Execution.Providers;

public class GeminiExecutionProvider : IAIExecutionProvider
{
    private readonly AiOptions _options;
    private readonly ILogger<GeminiExecutionProvider> _logger;

    public GeminiExecutionProvider(
        IOptions<AiOptions> options,
        ILogger<GeminiExecutionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Gemini";

    public async Task<Result<PromptExecutionResult>> ExecuteAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        CancellationToken cancellationToken)
    {
        var geminiOptions = _options.Providers.Gemini;
        var apiKey = !string.IsNullOrWhiteSpace(geminiOptions.ApiKey)
            ? geminiOptions.ApiKey
            : (Environment.GetEnvironmentVariable(geminiOptions.ApiKeyEnvironmentVariable)
               ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
               ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
               ?? Environment.GetEnvironmentVariable("AI__Providers__Gemini__ApiKey"));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var errMessage = $"Missing Gemini API key. Set environment variable '{geminiOptions.ApiKeyEnvironmentVariable}' or 'AI:Providers:Gemini:ApiKey' in User Secrets.";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[AI ERROR - GEMINI]: {errMessage}");
            Console.ResetColor();
            _logger.LogError("[AI ERROR - GEMINI]: {ErrorMessage}", errMessage);

            return Result.Failure<PromptExecutionResult>(new Error("ProviderError", errMessage, 500));
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n🚀 [AI MODEL REQUEST START] Provider: Gemini | Model: {target.ModelId} | Prompt: {prompt.Name}");
        Console.ResetColor();

        try
        {
            using var httpClient = new HttpClient();

            object input = request.File is null
                ? renderedPrompt
                : new object[]
                {
                    new
                    {
                        type = "user_input",
                        content = new object[]
                        {
                            new { type = "text", text = renderedPrompt },
                            new
                            {
                                type = ResolveGeminiInteractionFileType(request.File.ContentType),
                                data = Convert.ToBase64String(request.File.Content),
                                mime_type = request.File.ContentType
                            }
                        }
                    }
                };

            var payload = JsonSerializer.Serialize(new { model = target.ModelId, input });

            using var httpRequest =
                new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/v1/interactions");
            httpRequest.Headers.Add("x-goog-api-key", apiKey);
            httpRequest.Headers.Add("Api-Revision", "2026-05-20");
            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorInfo = $"[AI ERROR - GEMINI] Interactions API failed with status {(int)response.StatusCode}: {responseBody}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(errorInfo);
                Console.ResetColor();
                _logger.LogError("{ErrorInfo}", errorInfo);

                throw new HttpRequestException(errorInfo, null, response.StatusCode);
            }

            var raw = ExtractGeminiOutputText(responseBody);

            return Result<PromptExecutionResult>.Success(new PromptExecutionResult
            {
                PromptName = prompt.Name,
                PromptVersion = prompt.Version,
                Provider = target.ProviderName,
                ModelId = target.ModelId,
                RawResponse = raw
            });
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[AI ERROR - GEMINI MODEL REQUEST FAILED] Model: {target.ModelId} | Prompt: {prompt.Name} | Error: {ex.Message}");
            Console.ResetColor();
            _logger.LogError(ex, "[AI ERROR - GEMINI MODEL REQUEST FAILED] Model: {ModelId} | Prompt: {PromptName}", target.ModelId, prompt.Name);
            throw;
        }
    }

    private static string ExtractGeminiOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("steps", out var steps)) return string.Empty;

        foreach (var step in steps.EnumerateArray())
        {
            if (!step.TryGetProperty("type", out var type) || type.GetString() != "model_output" ||
                !step.TryGetProperty("content", out var content))
                continue;

            var textParts = content.EnumerateArray()
                .Where(part => part.TryGetProperty("type", out var partType) && partType.GetString() == "text" &&
                               part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text));

            return string.Join(Environment.NewLine, textParts);
        }

        return string.Empty;
    }

    private static string ResolveGeminiInteractionFileType(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) return "document";
        return "file";
    }

    public async IAsyncEnumerable<string> ExecuteStreamAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(target, request, prompt, renderedPrompt, cancellationToken);
        if (result.IsSuccess)
        {
            yield return result.Value!.RawResponse;
        }
        else
        {
            throw new InvalidOperationException($"Provider execution failed: {result.Error}");
        }
    }
}
