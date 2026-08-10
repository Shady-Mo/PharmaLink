using System.Runtime.CompilerServices;
using Application.Services.AI;
using Application.Services.AI.Models;

namespace Infrastructure.AI;

/// <summary>
/// Implements IPharmacyAssistantService using the new Orchestrator (IPromptExecutionService).
///
/// DESIGN DECISION — Scoped lifetime:
///   This service is Scoped (created once per HTTP request). It bridges the caller
///   to the AI Orchestrator, which handles resilience, fallback, and routing.
/// </summary>
public sealed class PharmacyAssistantService : IPharmacyAssistantService
{
    private readonly IPromptExecutionService _promptExecutionService;
    private readonly ILogger<PharmacyAssistantService> _logger;

    public PharmacyAssistantService(
        IPromptExecutionService promptExecutionService,
        ILogger<PharmacyAssistantService> logger)
    {
        _promptExecutionService = promptExecutionService;
        _logger = logger;
    }

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

        // Cap history length at 30 turns to keep enough context but avoid overflow
        var maxHistoryTurns = 30;
        var trimmedHistory = history.Count > maxHistoryTurns
            ? history.Skip(history.Count - maxHistoryTurns).ToList()
            : history.ToList();

        var request = new PromptExecutionRequest
        {
            PromptName = "PharmacyAssistant",
            TaskType = AITaskType.Chat,
            ChatHistory = trimmedHistory,
            UserMessage = userMessage,
            Variables = new Dictionary<string, object?>
            {
                { "current_date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC") },
                { "user_id", userId }
            }
        };

        try
        {
            var result = await _promptExecutionService.ExecuteAsync(request, ct);

            _logger.LogInformation(
                "PharmacyAssistantService.ChatAsync complete — User: {UserId}, ResponseLength: {Length}",
                userId, result.RawResponse.Length);

            return result.RawResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PharmacyAssistantService.ChatAsync failed for user {UserId}", userId);

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

        var maxHistoryTurns = 30;
        var trimmedHistory = history.Count > maxHistoryTurns
            ? history.Skip(history.Count - maxHistoryTurns).ToList()
            : history.ToList();

        var request = new PromptExecutionRequest
        {
            PromptName = "PharmacyAssistant",
            TaskType = AITaskType.Chat,
            ChatHistory = trimmedHistory,
            UserMessage = userMessage,
            Variables = new Dictionary<string, object?>
            {
                { "current_date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC") },
                { "user_id", userId }
            }
        };

        var stream = _promptExecutionService.ExecuteStreamAsync(request, ct);
        
        IAsyncEnumerator<string>? enumerator = null;
        string? initError = null;
        try
        {
            enumerator = stream.GetAsyncEnumerator(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PharmacyAssistantService.ChatStreamAsync failed to get enumerator for user {UserId}", userId);
            initError = "عذراً، أواجه ضغطاً حالياً في الاتصال بالخوادم. يرجى الانتظار دقيقة ثم المحاولة.";
        }

        if (initError != null)
        {
            yield return initError;
            yield break;
        }

        var yieldedAny = false;
        string? moveNextError = null;
        
        while (true)
        {
            try
            {
                if (!await enumerator!.MoveNextAsync())
                    break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PharmacyAssistantService.ChatStreamAsync failed during MoveNextAsync for user {UserId}", userId);
                if (!yieldedAny)
                    moveNextError = "عذراً، أواجه ضغطاً حالياً في الاتصال بالخوادم (429). يرجى الانتظار دقيقة ثم المحاولة.";
                break;
            }

            var text = enumerator!.Current;
            if (!string.IsNullOrEmpty(text))
            {
                yieldedAny = true;
                yield return text;
            }
        }
        
        await enumerator!.DisposeAsync();

        if (moveNextError != null)
        {
            yield return moveNextError;
            yield break;
        }

        if (!yieldedAny)
        {
            _logger.LogWarning("PharmacyAssistantService.ChatStreamAsync returned an empty response for user {UserId}", userId);
            yield return "عذراً، حدث انقطاع مفاجئ أثناء معالجة الرد (Empty Response). يرجى المحاولة مرة أخرى.";
        }

        _logger.LogInformation(
            "PharmacyAssistantService.ChatStreamAsync complete — User: {UserId}", userId);
    }
}
