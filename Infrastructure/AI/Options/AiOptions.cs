using Infrastructure.AI.Models;

namespace Infrastructure.AI.Options;

public class AiOptions
{
    public const string SectionName = "AI";
    public AIProviderDefaults Defaults { get; set; } = new();
    public PromptStoreOptions Prompts { get; set; } = new();
    public Dictionary<string, TaskRouteConfig> TaskRouting { get; set; } = new();
    public ProvidersOptions Providers { get; set; } = new();
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();
    public RetryPolicyConfig RetryPolicy { get; set; } = new();
}

public class TaskRouteConfig
{
    public string RoutingStrategy { get; set; } = "Priority"; // Priority, RoundRobin, Random, WeightedRoundRobin
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}

public class ProviderConfig
{
    public int Priority { get; set; } = 99;
    public int Weight { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public List<ModelConfig> Models { get; set; } = new();
    public RetryPolicyConfig? RetryPolicy { get; set; }
}

public class ModelConfig
{
    public string ModelId { get; set; } = string.Empty;
    public int Priority { get; set; } = 99;
    public int Weight { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30; public Dictionary<string, string[]> Models { get; set; } = new();
    public RetryPolicyConfig? RetryPolicy { get; set; }
    public string[] Tags { get; set; } = [];
}

public class CircuitBreakerConfig
{
    public int FailureThreshold { get; set; } = 3;
    public int CooldownSeconds { get; set; } = 60;
    public int HalfOpenMaxAttempts { get; set; } = 1;
}

public class RetryPolicyConfig
{
    public int RetryCount { get; set; } = 2;
    public int InitialDelayMs { get; set; } = 500;
    public int MaxDelayMs { get; set; } = 5000;
    public double BackoffMultiplier { get; set; } = 2.0;
}

public class AIProviderDefaults
{
    public string Provider { get; set; } = nameof(AIProvider.Gemini);
    public string VisionProvider { get; set; } = nameof(AIProvider.Gemini);
    public string ChatProvider { get; set; } = nameof(AIProvider.Gemini);
    public string EmbeddingProvider { get; set; } = nameof(AIProvider.ITI);
}

public class PromptStoreOptions
{
    public string BasePath { get; set; } = "AI/Prompts";
    public string DefaultVersion { get; set; } = "v1";
}

public class ProvidersOptions
{
    public GroqOptions Groq { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
    public GitHubModelsOptions GitHubModels { get; set; } = new();
    public ITIOptions ITI { get; set; } = new();
    public OpenRouterOptions OpenRouter { get; set; } = new();
}

public class GroqOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "GROQ_API_KEY"; public Dictionary<string, string[]> Models { get; set; } = new();
}

public class GeminiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "GEMINI_API_KEY";
    public int TimeoutSeconds { get; set; } = 30; public Dictionary<string, string[]> Models { get; set; } = new();
}

public class GitHubModelsOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string EmbeddingEndpoint { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty; public Dictionary<string, string[]> Models { get; set; } = new();
}

public class ITIOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "ITI_API_KEY";
    public int TimeoutSeconds { get; set; } = 60; public Dictionary<string, string[]> Models { get; set; } = new();
    public int MaxTokens { get; set; } = 1200;

    public string SystemPrompt { get; set; } =
        "You are a careful healthcare AI assistant. Return only valid JSON when the prompt asks for JSON.";
}

public class OpenRouterOptions
{
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "OPENROUTER_API_KEY";
    public string SiteUrl { get; set; } = "https://pharmalink.tryasp.net";
    public string SiteName { get; set; } = "PharmaLink";
    public int TimeoutSeconds { get; set; } = 60; public Dictionary<string, string[]> Models { get; set; } = new();
}
