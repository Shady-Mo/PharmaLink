using System.Net.Http.Headers;
using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Options;

namespace Infrastructure.AI.Execution.Providers;

public class ITIExecutionProvider : IAIExecutionProvider
{
    private readonly AiOptions _options;

    public ITIExecutionProvider(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public string ProviderName => "ITI";

    public async Task<Result<PromptExecutionResult>> ExecuteAsync(
        ProviderModelTarget target,
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        CancellationToken cancellationToken)
    {
        var itiOptions = _options.Providers.ITI;
        if (string.IsNullOrWhiteSpace(itiOptions.BaseUrl))
        {
            return Result.Failure<PromptExecutionResult>(new Error("ProviderError", "ITI BaseUrl is not configured.",
                500));
        }

        var apiKey = string.IsNullOrWhiteSpace(itiOptions.ApiKey)
            ? Environment.GetEnvironmentVariable(itiOptions.ApiKeyEnvironmentVariable) ??
              Environment.GetEnvironmentVariable("SBG_API_KEY")
            : itiOptions.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result.Failure<PromptExecutionResult>(new Error("ProviderError",
                "Missing ITI API key. Set environment variable '{itiOptions.ApiKeyEnvironmentVariable}'.", 500));
        }

        var payload = request.File is null
            ? CreateITIChatPayload(target.ModelId, renderedPrompt, itiOptions)
            : CreateITIMultimodalPayload(target.ModelId, renderedPrompt, request.File, itiOptions);

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(itiOptions.BaseUrl)),
            Timeout = TimeSpan.FromSeconds(
                target.TimeoutSeconds > 0 ? target.TimeoutSeconds : itiOptions.TimeoutSeconds)
        };

        var endpoint = request.File is null ? "student/chat" : "student/multimodal-chat";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"ITI chat API failed with status {(int)response.StatusCode}: {responseBody}", null,
                response.StatusCode);
        }

        var raw = ExtractITIOutputText(responseBody);

        return Result<PromptExecutionResult>.Success(new PromptExecutionResult
        {
            PromptName = prompt.Name,
            PromptVersion = prompt.Version,
            Provider = target.ProviderName,
            ModelId = target.ModelId,
            RawResponse = raw
        });
    }

    private static string CreateITIChatPayload(string modelId, string renderedPrompt, ITIOptions options)
    {
        return JsonSerializer.Serialize(new
        {
            model_id = modelId,
            messages = new[] { new { role = "user", content = renderedPrompt } },
            system_prompt = options.SystemPrompt,
            max_tokens = options.MaxTokens
        });
    }

    private static string CreateITIMultimodalPayload(string modelId, string renderedPrompt, AIFileContent file,
        ITIOptions options)
    {
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"ITI multimodal-chat currently supports image files only. Received content type '{file.ContentType}'.");
        }

        return JsonSerializer.Serialize(new
        {
            model_id = modelId,
            messages = new[]
            {
                new
                {
                    role = "user",
                    text = renderedPrompt,
                    images = new[]
                    {
                        new
                        {
                            format = ResolveITIImageFormat(file.ContentType),
                            data_base64 = Convert.ToBase64String(file.Content)
                        }
                    }
                }
            },
            max_tokens = options.MaxTokens
        });
    }

    private static string ResolveITIImageFormat(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpeg"
        };
    }

    private static string ExtractITIOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (TryGetString(root, "content", out var content) || TryGetString(root, "output_text", out content) ||
            TryGetString(root, "text", out content) || TryGetString(root, "response", out content))
            return content;

        if (root.TryGetProperty("message", out var message) && TryGetString(message, "content", out content))
            return content;

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var choiceMessage) &&
                    TryGetString(choiceMessage, "content", out content)) return content;
                if (TryGetString(choice, "text", out content)) return content;
            }
        }

        return responseBody;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)) return false;

        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        if (property.ValueKind == JsonValueKind.Array)
        {
            var textParts = property.EnumerateArray()
                .Select(part =>
                {
                    if (part.ValueKind == JsonValueKind.String) return part.GetString();
                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        return text.GetString();
                    return null;
                })
                .Where(text => !string.IsNullOrWhiteSpace(text));

            value = string.Join(Environment.NewLine, textParts);
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static string EnsureTrailingSlash(string value) => value.EndsWith('/') ? value : $"{value}/";

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