namespace Infrastructure.AI.Options;

public class AiOptions
{
    public const string SectionName = "AI";
    public AIProviderDefaults Defaults { get; set; } = new();
    public PromptStoreOptions Prompts { get; set; } = new();
    public Dictionary<string, AIProviderSelectionOptions> TaskRouting { get; set; } = new();
    public ProvidersOptions Providers { get; set; } = new();
}

public class AIProviderDefaults
{
    public string Provider { get; set; } = nameof(Models.AIProvider.Gemini);
    public string VisionProvider { get; set; } = nameof(Models.AIProvider.Gemini);
    public string ChatProvider { get; set; } = nameof(Models.AIProvider.Gemini);
    public string EmbeddingProvider { get; set; } = nameof(Models.AIProvider.ITI);
}

public class PromptStoreOptions
{
    public string BasePath { get; set; } = "AI/Prompts";
    public string DefaultVersion { get; set; } = "v1";
}

public class AIProviderSelectionOptions
{
    public string Provider { get; set; } = string.Empty;
    public string? ModelId { get; set; }
}

public class ProvidersOptions
{
    public GroqOptions Groq { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
    public GitHubModelsOptions GitHubModels { get; set; } = new();
    public ITIOptions ITI { get; set; } = new();
}

public class GroqOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "GROQ_API_KEY";
    public Dictionary<string, string[]> Models { get; set; } = new();
}

public class GeminiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "GEMINI_API_KEY";
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string[]> Models { get; set; } = new();
}

public class GitHubModelsOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string EmbeddingEndpoint { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Dictionary<string, string[]> Models { get; set; } = new();
}

public class ITIOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "ITI_API_KEY";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxTokens { get; set; } = 1200;
    public string SystemPrompt { get; set; } = "You are a careful healthcare AI assistant. Return only valid JSON when the prompt asks for JSON.";
    public Dictionary<string, string[]> Models { get; set; } = new();
}
