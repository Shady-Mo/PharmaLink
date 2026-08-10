using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Options;

namespace Infrastructure.AI.Execution.Providers;

public class GeminiExecutionProvider : IAIExecutionProvider
{
    private readonly AiOptions _options;

    public GeminiExecutionProvider(IOptions<AiOptions> options)
    {
        _options = options.Value;
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
        var apiKey = string.IsNullOrWhiteSpace(geminiOptions.ApiKey)
            ? Environment.GetEnvironmentVariable(geminiOptions.ApiKeyEnvironmentVariable)
            : geminiOptions.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result.Failure<PromptExecutionResult>(new Error("ProviderError",
                "Missing Gemini API key. Set environment variable '{geminiOptions.ApiKeyEnvironmentVariable}'.", 500));
        }

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
            // Throw so that the Polly pipeline catches it as an HttpOperationException or similar if we wanted,
            // or just throw a custom HttpRequestException so IsTransient catches it.
            throw new HttpRequestException(
                $"Gemini Interactions API failed with status {(int)response.StatusCode}: {responseBody}", null,
                response.StatusCode);
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

