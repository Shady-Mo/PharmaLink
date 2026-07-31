using System.Diagnostics;
using System.Net.Http.Headers;
using Application.Services.AI;
using Application.Services.AI.Models;
using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Infrastructure.AI.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.AI.Execution;

public class SemanticKernelPromptExecutionService(
    IKernelFactory kernelFactory,
    IAIProviderSelector providerSelector,
    IPromptRegistry promptRegistry,
    ILogger<SemanticKernelPromptExecutionService> logger,
    IOptions<AiOptions> options)
    : IPromptExecutionService
{
    private readonly AiOptions _aiOptions = options.Value;

    public async Task<PromptExecutionResult> ExecuteAsync(
        PromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = await promptRegistry.GetAsync(
            request.PromptName,
            request.PromptVersion,
            cancellationToken);

        var selection = providerSelector.Select(request.TaskType, request.PromptName);
        var renderedPrompt = Render(prompt.Template, request.Variables);

        if (selection.Provider == AIProvider.ITI)
        {
            return await ExecuteITIChatAsync(
                request,
                prompt,
                renderedPrompt,
                selection.Role,
                selection.ModelId,
                cancellationToken);
        }

        if (selection.Provider == AIProvider.Gemini)
        {
            return await ExecuteGeminiInteractionAsync(
                request,
                prompt,
                renderedPrompt,
                selection.ModelId ?? _aiOptions.Providers.Gemini.Models[selection.Role.ToString()][0],
                cancellationToken);
        }

        var kernel = kernelFactory.GetKernel(selection.Provider, selection.Role, selection.ModelId);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();

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

        var stopwatch = Stopwatch.StartNew();
        var response = await chatService.GetChatMessageContentAsync(
            history,
            kernel: kernel,
            cancellationToken: cancellationToken);
        stopwatch.Stop();

        var raw = response.Content ?? string.Empty;
        logger.LogInformation(
            "AI prompt executed. Prompt={PromptName} Version={PromptVersion} Provider={Provider} Model={ModelId} LatencyMs={LatencyMs}",
            prompt.Name,
            prompt.Version,
            selection.Provider,
            selection.ModelId,
            stopwatch.ElapsedMilliseconds);

        return new PromptExecutionResult
        {
            PromptName = prompt.Name,
            PromptVersion = prompt.Version,
            Provider = selection.Provider.ToString(),
            ModelId = selection.ModelId ?? string.Empty,
            RawResponse = raw,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<PromptExecutionResult> ExecuteITIChatAsync(
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        ModelRole role,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var itiOptions = _aiOptions.Providers.ITI;
        var roleName = role.ToString();

        if (!_aiOptions.Providers.ITI.Models.TryGetValue(roleName, out var configuredModels)
            || configuredModels.Length == 0)
        {
            throw new InvalidOperationException($"Model for role {roleName} is not configured for ITI.");
        }

        var selectedModelId = modelId ?? configuredModels[0];
        if (!configuredModels.Contains(selectedModelId))
        {
            throw new InvalidOperationException(
                $"Model {selectedModelId} is not configured for role {roleName} in ITI.");
        }

        if (string.IsNullOrWhiteSpace(itiOptions.BaseUrl))
        {
            throw new InvalidOperationException("ITI BaseUrl is not configured.");
        }

        var apiKey = string.IsNullOrWhiteSpace(itiOptions.ApiKey)
            ? Environment.GetEnvironmentVariable(itiOptions.ApiKeyEnvironmentVariable)
              ?? Environment.GetEnvironmentVariable("SBG_API_KEY")
            : itiOptions.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Missing ITI API key. Set environment variable '{itiOptions.ApiKeyEnvironmentVariable}' or 'SBG_API_KEY'.");
        }

        var payload = request.File is null
            ? CreateITIChatPayload(selectedModelId, renderedPrompt, itiOptions)
            : CreateITIMultimodalPayload(selectedModelId, renderedPrompt, request.File, itiOptions);

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(itiOptions.BaseUrl)),
            Timeout = TimeSpan.FromSeconds(itiOptions.TimeoutSeconds)
        };

        var endpoint = request.File is null ? "student/chat" : "student/multimodal-chat";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"ITI chat API failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var raw = ExtractITIOutputText(responseBody);
        logger.LogInformation(
            "AI prompt executed. Prompt={PromptName} Version={PromptVersion} Provider={Provider} Model={ModelId} LatencyMs={LatencyMs}",
            prompt.Name,
            prompt.Version,
            AIProvider.ITI,
            selectedModelId,
            stopwatch.ElapsedMilliseconds);

        return new PromptExecutionResult
        {
            PromptName = prompt.Name,
            PromptVersion = prompt.Version,
            Provider = AIProvider.ITI.ToString(),
            ModelId = selectedModelId,
            RawResponse = raw,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };
    }

    private static string CreateITIChatPayload(
        string modelId,
        string renderedPrompt,
        ITIOptions options)
    {
        return JsonSerializer.Serialize(new
        {
            model_id = modelId,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = renderedPrompt
                }
            },
            system_prompt = options.SystemPrompt,
            max_tokens = options.MaxTokens
        });
    }

    private static string CreateITIMultimodalPayload(
        string modelId,
        string renderedPrompt,
        AIFileContent file,
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

        if (TryGetString(root, "content", out var content)
            || TryGetString(root, "output_text", out content)
            || TryGetString(root, "text", out content)
            || TryGetString(root, "response", out content))
        {
            return content;
        }

        if (root.TryGetProperty("message", out var message)
            && TryGetString(message, "content", out content))
        {
            return content;
        }

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var choiceMessage)
                    && TryGetString(choiceMessage, "content", out content))
                {
                    return content;
                }

                if (TryGetString(choice, "text", out content))
                {
                    return content;
                }
            }
        }

        return responseBody;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        if (property.ValueKind == JsonValueKind.Array)
        {
            var textParts = property
                .EnumerateArray()
                .Select(part =>
                {
                    if (part.ValueKind == JsonValueKind.String)
                    {
                        return part.GetString();
                    }

                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        return text.GetString();
                    }

                    return null;
                })
                .Where(text => !string.IsNullOrWhiteSpace(text));

            value = string.Join(Environment.NewLine, textParts);
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : $"{value}/";
    }

    private async Task<PromptExecutionResult> ExecuteGeminiInteractionAsync(
        PromptExecutionRequest request,
        PromptDefinition prompt,
        string renderedPrompt,
        string modelId,
        CancellationToken cancellationToken)
    {
        var geminiOptions = _aiOptions.Providers.Gemini;
        var apiKey = string.IsNullOrWhiteSpace(geminiOptions.ApiKey)
            ? Environment.GetEnvironmentVariable(geminiOptions.ApiKeyEnvironmentVariable)
            : geminiOptions.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Missing Gemini API key. Set environment variable '{geminiOptions.ApiKeyEnvironmentVariable}'.");
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
                        new
                        {
                            type = "text",
                            text = renderedPrompt
                        },
                        new
                        {
                            type = ResolveGeminiInteractionFileType(request.File.ContentType),
                            data = Convert.ToBase64String(request.File.Content),
                            mime_type = request.File.ContentType
                        }
                    }
                }
            };

        var payload = JsonSerializer.Serialize(new
        {
            model = modelId,
            input
        });

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1/interactions");

        httpRequest.Headers.Add("x-goog-api-key", apiKey);
        httpRequest.Headers.Add("Api-Revision", "2026-05-20");
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Gemini Interactions API failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var raw = ExtractGeminiOutputText(responseBody);
        logger.LogInformation(
            "AI prompt executed. Prompt={PromptName} Version={PromptVersion} Provider={Provider} Model={ModelId} LatencyMs={LatencyMs}",
            prompt.Name,
            prompt.Version,
            AIProvider.Gemini,
            modelId,
            stopwatch.ElapsedMilliseconds);

        return new PromptExecutionResult
        {
            PromptName = prompt.Name,
            PromptVersion = prompt.Version,
            Provider = AIProvider.Gemini.ToString(),
            ModelId = modelId,
            RawResponse = raw,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };
    }

    private static string ExtractGeminiOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("steps", out var steps))
        {
            return string.Empty;
        }

        foreach (var step in steps.EnumerateArray())
        {
            if (!step.TryGetProperty("type", out var type)
                || type.GetString() != "model_output"
                || !step.TryGetProperty("content", out var content))
            {
                continue;
            }

            var textParts = content
                .EnumerateArray()
                .Where(part => part.TryGetProperty("type", out var partType)
                    && partType.GetString() == "text"
                    && part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text));

            return string.Join(Environment.NewLine, textParts);
        }

        return string.Empty;
    }

    private static string ResolveGeminiInteractionFileType(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "document";
        }

        return "file";
    }

    private static string Render(string template, IReadOnlyDictionary<string, object?> variables)
    {
        var rendered = template;
        foreach (var (key, value) in variables)
        {
            rendered = rendered.Replace($"{{{{{key}}}}}", value?.ToString() ?? string.Empty);
        }

        return rendered;
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
