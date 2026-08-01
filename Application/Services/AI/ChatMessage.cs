namespace Application.Services.AI;

/// <summary>
/// Represents a single message in a conversation.
///
/// Design decision: This is a plain record with no Semantic Kernel
/// references. The Application layer defines the contract; the
/// Infrastructure layer maps this to SK's ChatHistory internally.
/// This preserves Clean Architecture — swapping SK for another
/// framework requires no changes to the Application layer.
/// </summary>
/// <param name="Role">Who sent the message: "user", "assistant", or "system".</param>
/// <param name="Content">The text content of the message.</param>
public sealed record ChatMessage(string Role, string Content)
{
    /// <summary>Creates a user message.</summary>
    public static ChatMessage FromUser(string content) => new("user", content);

    /// <summary>Creates an assistant message.</summary>
    public static ChatMessage FromAssistant(string content) => new("assistant", content);

    /// <summary>Creates a system message.</summary>
    public static ChatMessage FromSystem(string content) => new("system", content);
}
