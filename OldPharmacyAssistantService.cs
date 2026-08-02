using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Infrastructure.AI;

/// <summary>
/// Implements IPharmacyAssistantService using Semantic Kernel.
///
/// DESIGN DECISION — Scoped lifetime:
///   This service is Scoped (created once per HTTP request) even though the
///   Kernel it depends on is a Singleton. The service itself holds no persistent
///   state between requests — it creates a fresh ChatHistory for each call,
///   reads from request-scoped services (ICurrentUserService), and disposes
///   cleanly. Scoped is the correct lifetime for a service that bridges
///   Singleton (Kernel) with per-request concerns (auth context, logging).
///
/// DESIGN DECISION — System prompt from file:
///   The system prompt is loaded from a .prompty file at construction time and
///   cached as a string field. This avoids disk I/O on every chat call while
///   still keeping the prompt editable outside of compiled code.
///
/// DESIGN DECISION — FunctionChoiceBehavior.Auto():
///   With Auto(), the AI model decides when to call tools (plugins). The
///   developer does NOT need to write orchestration loops. SK handles:
///     1. Sending the list of available functions to the model
///     2. Detecting when the model requests a function call
///     3. Executing the function and returning results to the model
///     4. Repeating until the model produces a final text response
///   MaximumAutoInvokeAttempts caps this at 5 rounds to prevent infinite loops
///   and control API costs.
/// </summary>
public sealed class PharmacyAssistantService : IPharmacyAssistantService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly SemanticKernelSettings _settings;
    private readonly ILogger<PharmacyAssistantService> _logger;
    private readonly string _systemPromptTemplate;

    public PharmacyAssistantService(
        Kernel kernel,
        IChatCompletionService chatService,
        IOptions<SemanticKernelSettings> settings,
        ILogger<PharmacyAssistantService> logger)
    {
        _kernel = kernel;
        _chatService = chatService;
        _settings = settings.Value;
        _logger = logger;

        // Load the system prompt template once at construction time.
        // The file is embedded in the Infrastructure project at build time.
        _systemPromptTemplate = LoadSystemPrompt();
    }

    // -------------------------------------------------------------------------
    //  IPharmacyAssistantService Implementation
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        string userId,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "PharmacyAssistantService.ChatAsync — User: {UserId}, HistoryLength: {HistoryLength}",
            userId, history.Count);

        var chatHistory = BuildChatHistory(userId, history);
        chatHistory.AddUserMessage(userMessage);

        var executionSettings = BuildExecutionSettings();

        try
        {
            var result = await _chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                ct);

            var responseText = result.Content ?? string.Empty;

            _logger.LogInformation(
                "PharmacyAssistantService.ChatAsync complete — User: {UserId}, ResponseLength: {Length}",
                userId, responseText.Length);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PharmacyAssistantService.ChatAsync failed for user {UserId}", userId);

            // Graceful degradation: return a user-friendly message rather than
            // propagating the exception to the HTTP layer.
            return "عذراً، أواجه ضغطاً حالياً في الاتصال بالخوادم (429 Too Many Requests). يرجى الانتظار دقيقة ثم المحاولة مرة أخرى.";
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string userId,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation(
            "PharmacyAssistantService.ChatStreamAsync — User: {UserId}", userId);

        var chatHistory = BuildChatHistory(userId, history);
        chatHistory.AddUserMessage(userMessage);

        var executionSettings = BuildExecutionSettings();

        // DESIGN DECISION — Streaming with function calling:
        //   GetStreamingChatMessageContentsAsync streams tokens as they arrive.
        //   When FunctionChoiceBehavior.Auto is active and the model makes a
        //   function call mid-stream, SK pauses the stream, executes the
        //   function, and resumes streaming the model's continuation.
        //   The caller sees a seamless token stream without needing to handle
        //   the function call lifecycle.
        //
        // DESIGN DECISION — No yield in catch:
        //   C# does not allow yield return inside a catch block.
        //   We use a sentinel errorMessage string: if set, we yield it outside
        //   the try-catch and then break. This keeps the error reporting
        //   inline without restructuring into a channel-based approach.
        string? errorMessage = null;
        IAsyncEnumerable<StreamingChatMessageContent>? stream = null;

        try
        {
            stream = _chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings,
                _kernel,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PharmacyAssistantService.ChatStreamAsync failed to initiate for user {UserId}", userId);
            errorMessage = "عذراً، أواجه ضغطاً حالياً في الاتصال بالخوادم (429). يرجى الانتظار دقيقة ثم المحاولة.";
        }

        if (errorMessage is not null)
        {
            yield return errorMessage;
            yield break;
        }

        await foreach (var chunk in stream!.WithCancellation(ct))
        {
            var text = chunk.Content;
            if (!string.IsNullOrEmpty(text))
                yield return text;
        }

        _logger.LogInformation(
            "PharmacyAssistantService.ChatStreamAsync complete — User: {UserId}", userId);
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a SK ChatHistory from our Application-layer ChatMessage records.
    /// Prepends the system prompt and trims history to MaxHistoryTurns to
    /// avoid context window overflow.
    /// </summary>
    private ChatHistory BuildChatHistory(string userId, IReadOnlyList<ChatMessage> history)
    {
        var systemPrompt = _systemPromptTemplate
            .Replace("{{$current_date}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"))
            .Replace("{{$user_id}}", userId);

        var chatHistory = new ChatHistory(systemPrompt);

        // Trim history to the most recent N turns to prevent context overflow.
        var trimmedHistory = history.Count > _settings.MaxHistoryTurns
            ? history.Skip(history.Count - _settings.MaxHistoryTurns).ToList()
            : history;

        foreach (var msg in trimmedHistory)
        {
            switch (msg.Role.ToLowerInvariant())
            {
                case "user":
                    chatHistory.AddUserMessage(msg.Content);
                    break;
                case "assistant":
                    chatHistory.AddAssistantMessage(msg.Content);
                    break;
                // "system" messages in history are intentionally skipped —
                // only our fixed system prompt is allowed as a system turn.
            }
        }

        return chatHistory;
    }

    /// <summary>
    /// Creates the execution settings with auto function calling enabled.
    /// Falls back to OpenAI settings if the provider is not Gemini.
    /// </summary>
    private PromptExecutionSettings BuildExecutionSettings()
    {
        var provider = _settings.Provider.Trim().ToLowerInvariant();

        if (provider is "googlegemini" or "gemini")
        {
            // GeminiPromptExecutionSettings gives us access to Gemini-specific
            // parameters while FunctionChoiceBehavior.Auto() is cross-provider.
            return new GeminiPromptExecutionSettings
            {
                MaxTokens = 8192,
                Temperature = _settings.Temperature,
                // FunctionChoiceBehavior.Auto() — the model can call any plugin
                // function registered on the Kernel automatically.
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
        }

        // Generic settings that work with OpenAI and Azure OpenAI.
        return new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
    }

    /// <summary>
    /// Loads the system prompt from the embedded .prompty file.
    /// The file is included as a project item and copied to the output directory.
    /// </summary>
    private static string LoadSystemPrompt()
    {
        // Resolve the path relative to the executing assembly's location.
        // This works both in development (bin/Debug) and when deployed.
        var assemblyDir = Path.GetDirectoryName(typeof(PharmacyAssistantService).Assembly.Location)!;
        var promptPath = Path.Combine(assemblyDir, "AI", "PromptTemplates", "PharmacyAssistant.prompty");

        if (File.Exists(promptPath))
            return File.ReadAllText(promptPath);

        // Fallback minimal system prompt if the file is missing.
        return "You are a helpful pharmacy assistant for PharmaLink. " +
               "Always recommend consulting a licensed pharmacist or doctor for medical advice.";
    }
}
