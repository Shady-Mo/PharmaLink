namespace Application.Settings;

/// <summary>
/// Strongly-typed settings bound from the "OtpWebhook" section of appsettings.json.
/// </summary>
public class OtpWebhookSettings
{
    public const string SectionName = "OtpWebhook";

    /// <summary>
    /// The n8n webhook URL that receives the OTP dispatch payload.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// HTTP request timeout in seconds. Defaults to 10.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
