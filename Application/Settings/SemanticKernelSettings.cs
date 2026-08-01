using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

/// <summary>
/// Configuration options for the Semantic Kernel AI orchestration layer.
/// Bind this to the "SemanticKernel" section in appsettings.json.
///
/// SECURITY: ApiKey is NEVER stored in appsettings.json.
/// Use dotnet user-secrets (dev) or the environment variable
/// SemanticKernel__ApiKey (production) instead.
/// </summary>
public sealed class SemanticKernelSettings
{
    public const string SectionName = "SemanticKernel";

    /// <summary>
    /// The AI provider to use. Supported values: "GoogleGemini", "OpenAI", "AzureOpenAI".
    /// </summary>
    [Required]
    public string Provider { get; set; } = "GoogleGemini";

    /// <summary>
    /// The model identifier for the chosen provider.
    /// Examples: "gemini-3.5-flash", "gpt-4o", "gpt-4o-mini".
    /// </summary>
    [Required]
    public string ModelId { get; set; } = "gemini-3.5-flash";

    /// <summary>
    /// API key for the AI provider.
    /// Loaded from User Secrets (dev) or environment variable SemanticKernel__ApiKey (prod).
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI endpoint (only used when Provider = "AzureOpenAI").
    /// Example: "https://my-resource.openai.azure.com/"
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Maximum number of tokens to generate per response.
    /// </summary>
    [Range(1, 32768)]
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Sampling temperature. 0 = deterministic, 1 = creative.
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of conversation history turns to send to the model.
    /// Prevents context window overflow on long conversations.
    /// </summary>
    [Range(1, 50)]
    public int MaxHistoryTurns { get; set; } = 10;

    /// <summary>
    /// Settings for the semantic memory / vector store.
    /// </summary>
    public SemanticMemorySettings Memory { get; set; } = new();
}

/// <summary>
/// Configuration for the in-memory vector store used for RAG (Retrieval-Augmented Generation).
/// </summary>
public sealed class SemanticMemorySettings
{
    /// <summary>
    /// Whether to enable the in-memory semantic text memory.
    /// When disabled, all memory-related operations are no-ops.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The name of the memory collection to use.
    /// </summary>
    public string CollectionName { get; set; } = "pharmalink-docs";
}