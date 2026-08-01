namespace Application.Services.AI;

/// <summary>
/// Provides an AI-powered pharmacy assistant that can answer medication questions
/// and engage in multi-turn conversations using the registered AI backend.
///
/// Design decision: This interface lives in the Application layer and contains
/// ZERO Semantic Kernel references. The Infrastructure layer implements it
/// using SK. This enforces the Dependency Inversion Principle — higher-level
/// modules (Application, API) depend on abstractions, not SK concretions.
///
/// Callers pass the full conversation history so the service remains stateless.
/// This avoids server-side session state and makes the service horizontally scalable.
/// </summary>
public interface IPharmacyAssistantService
{
    /// <summary>
    /// Sends a message to the AI assistant and returns the full response.
    /// The assistant has access to native plugins (drug database, inventory)
    /// and will call them automatically as needed.
    /// </summary>
    /// <param name="userId">The authenticated user's ID — used for audit logging.</param>
    /// <param name="history">Prior conversation turns (newest last).</param>
    /// <param name="userMessage">The user's latest message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assistant's full response text.</returns>
    Task<string> ChatAsync(
        string userId,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a message and streams the response token-by-token using IAsyncEnumerable.
    ///
    /// Design decision: Streaming is provided via IAsyncEnumerable&lt;string&gt; rather
    /// than a callback/event to fit naturally into ASP.NET Core's SSE (Server-Sent
    /// Events) pattern and async LINQ. The caller uses "await foreach" and writes
    /// each chunk to the HTTP response stream, giving users immediate feedback
    /// without waiting for the full response.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="history">Prior conversation turns.</param>
    /// <param name="userMessage">The user's latest message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of response text chunks.</returns>
    IAsyncEnumerable<string> ChatStreamAsync(
        string userId,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken ct = default);
}
