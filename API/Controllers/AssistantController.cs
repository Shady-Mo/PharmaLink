using System.Net.Mime;
using System.Text;
using API.Extensions;
using Application.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Provides AI-powered pharmacy assistant endpoints.
/// All endpoints require authentication — the user's identity is used for
/// audit logging and for scoping order data queries.
/// </summary>
[Authorize]
[Route("api/v1/assistant")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public sealed class AssistantController(
    IPharmacyAssistantService assistantService,
    IDrugInfoService drugInfoService,
    ILogger<AssistantController> logger) : ControllerBase
{
    // -------------------------------------------------------------------------
    //  Chat Endpoints
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sends a message to the AI pharmacy assistant and receives a full response.
    /// The assistant has access to the drug database and inventory and will
    /// automatically query them as needed to answer your question.
    /// </summary>
    /// <remarks>
    /// Send the conversation history in chronological order (oldest first).
    /// The server is stateless — you are responsible for maintaining history on the client.
    ///
    /// Example request:
    ///
    ///     POST /api/v1/assistant/chat
    ///     {
    ///       "message": "What is Amoxicillin used for?",
    ///       "history": []
    ///     }
    ///
    /// </remarks>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var id = User.GetUserId();
        var userId = id == Guid.Empty ? "anonymous" : id.ToString();

        logger.LogInformation(
            "AssistantController.Chat — User {UserId}, Message length: {Length}",
            userId, request.Message.Length);

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message cannot be empty." });

        var response = await assistantService.ChatAsync(
            userId,
            request.History ?? [],
            request.Message,
            ct);

        return Ok(new ChatResponse(response, userId));
    }

    /// <summary>
    /// Streams the AI assistant's response token-by-token using Server-Sent Events (SSE).
    /// Suitable for real-time chat interfaces where users should see the response
    /// as it is being generated.
    /// </summary>
    /// <remarks>
    /// The client must set the Accept header to "text/event-stream".
    /// Each SSE event's data field contains one text chunk.
    /// A final event with data "[DONE]" signals the end of the stream.
    ///
    /// Example request:
    ///
    ///     POST /api/v1/assistant/chat/stream
    ///     {
    ///       "message": "Is Augmentin available at any PharmaLink branch?",
    ///       "history": []
    ///     }
    ///
    /// </remarks>
    [HttpPost("chat/stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task ChatStream(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var id = User.GetUserId();
        var userId = id == Guid.Empty ? "anonymous" : id.ToString();

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Configure the response as an SSE stream.
        // DESIGN DECISION: We use text/event-stream (SSE) rather than WebSockets
        // because SSE is simpler (HTTP, unidirectional), works through proxies and
        // load balancers, and is sufficient for AI streaming (server → client only).
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disables nginx buffering

        try
        {
            await foreach (var chunk in assistantService.ChatStreamAsync(
                               userId,
                               request.History ?? [],
                               request.Message,
                               ct))
            {
                // SSE format: "data: <content>\n\n"
                var sseData = $"data: {EscapeForSse(chunk)}\n\n";
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(sseData), ct);
                await Response.Body.FlushAsync(ct);
            }

            // Signal end of stream
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("data: [DONE]\n\n"), ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — this is normal, not an error.
            logger.LogDebug("ChatStream cancelled for user {UserId} (client disconnected)", userId);
        }
        catch (Exception ex) when (ex.ToString().Contains("429"))
        {
            logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
            var errorData = "data: عذراً، لقد تجاوزت الحد الأقصى للطلبات المجانية للذكاء الاصطناعي. يرجى الانتظار لمدة دقيقة والمحاولة مرة أخرى.\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(errorData), ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatStream error for user {UserId}", userId);
            var errorData = "data: حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(errorData), ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    // -------------------------------------------------------------------------
    //  Drug Information Endpoints
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retrieves structured, AI-enriched information about a specific drug.
    /// Combines data from the PharmaLink drug database with the AI model's
    /// medical knowledge to produce a comprehensive drug profile.
    /// </summary>
    /// <param name="drugName">The drug name (brand or generic, partial names accepted).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("drug-info")]
    [ProducesResponseType(typeof(DrugInfoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDrugInfo(
        [FromQuery] string drugName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(drugName))
            return BadRequest(new { error = "Drug name is required." });

        logger.LogInformation("AssistantController.GetDrugInfo for: {DrugName}", drugName);

        try
        {
            var result = await drugInfoService.GetDrugInfoAsync(drugName, ct);

            if (result is null)
                return NotFound(new { error = $"Could not retrieve information for '{drugName}'." });

            return Ok(result);
        }
        catch (Exception ex) when (ex.ToString().Contains("429"))
        {
            return StatusCode(429, new { error = "الذكاء الاصطناعي مشغول حالياً بسبب كثرة الطلبات. يرجى الانتظار قليلاً والمحاولة مرة أخرى." });
        }
    }

    /// <summary>
    /// Checks for known drug-drug interactions between a list of medications.
    /// Returns a structured report with severity levels and clinical recommendations.
    /// </summary>
    /// <remarks>
    /// Provide at least 2 drug names. The result includes all detected interactions,
    /// their severity, and an overall safety summary.
    ///
    /// Example request:
    ///
    ///     POST /api/v1/assistant/check-interactions
    ///     {
    ///       "drugNames": ["Warfarin", "Aspirin", "Ibuprofen"]
    ///     }
    ///
    /// </remarks>
    [HttpPost("check-interactions")]
    [ProducesResponseType(typeof(InteractionCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckInteractions(
        [FromBody] CheckInteractionsRequest request,
        CancellationToken ct)
    {
        if (request.DrugNames is null || request.DrugNames.Count < 2)
            return BadRequest(new { error = "At least 2 drug names are required for an interaction check." });

        logger.LogInformation(
            "AssistantController.CheckInteractions for {Count} drugs",
            request.DrugNames.Count);

        try
        {
            var result = await drugInfoService.CheckInteractionsAsync(request.DrugNames, ct);
            return Ok(result);
        }
        catch (Exception ex) when (ex.ToString().Contains("429"))
        {
            return StatusCode(429, new { error = "الذكاء الاصطناعي مشغول حالياً بسبب كثرة الطلبات. يرجى الانتظار قليلاً والمحاولة مرة أخرى." });
        }
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private static string EscapeForSse(string text) =>
        // SSE data must not contain unescaped newlines — replace with spaces.
        text.Replace("\n", " ").Replace("\r", "");
}

// ─── Request / Response DTOs ───────────────────────────────────────────────────
// These are thin API-layer DTOs — not domain objects.

/// <summary>Request body for chat endpoints.</summary>
public sealed class ChatRequest
{
    /// <summary>The user's latest message.</summary>
    public required string Message { get; init; }

    /// <summary>Prior conversation history (oldest first). Can be empty for a new conversation.</summary>
    public IReadOnlyList<ChatMessage>? History { get; init; }
}

/// <summary>Response from the non-streaming chat endpoint.</summary>
public sealed record ChatResponse(string Reply, string UserId);

/// <summary>Request body for the interaction check endpoint.</summary>
public sealed class CheckInteractionsRequest
{
    /// <summary>List of drug names to check (minimum 2).</summary>
    public IReadOnlyList<string>? DrugNames { get; init; }
}