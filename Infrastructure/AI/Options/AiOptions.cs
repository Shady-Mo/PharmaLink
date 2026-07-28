namespace Infrastructure.AI.Options;

public class AiOptions
{
    public const string SectionName = "AI";
    public ProvidersOptions Providers { get; set; } = new();
}

public class ProvidersOptions
{
    public GroqOptions Groq { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
    public GitHubModelsOptions GitHubModels { get; set; } = new();
    public AwsBedrockOptions AwsBedrock { get; set; } = new();
}

public class GroqOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, string[]> Models { get; set; } = new();
}

public class GeminiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
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

public class AwsBedrockOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, string[]> Models { get; set; } = new();
}