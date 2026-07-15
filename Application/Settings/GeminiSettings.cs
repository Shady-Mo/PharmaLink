namespace Application.Settings;

public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string ModelName { get; set; } = "gemini-3.5-flash";

    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";

    public int TimeoutSeconds { get; set; } = 30;
}
