using System.Web;

namespace Infrastructure.Services;

/// <summary>
/// Delivers OTP codes to users by sending an HTTP GET to the configured n8n webhook URL.
/// The <c>phoneNumber</c> and <c>otp</c> are appended as URL query string parameters.
/// Uses <see cref="IHttpClientFactory"/> for safe socket management and resilient HTTP behaviour.
/// </summary>
public class WebhookOtpDispatcher(
    IHttpClientFactory httpClientFactory,
    IOptions<OtpWebhookSettings> options,
    ILogger<WebhookOtpDispatcher> logger) : IWebhookOtpDispatcher
{
    /// <summary>Named HttpClient key registered in DI.</summary>
    public const string HttpClientName = "OtpWebhook";

    public async Task DispatchAsync(string phoneNumber, string otp)
    {
        var settings = options.Value;

        // Build the GET URL: <baseUrl>?phoneNumber=...&otp=...
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["phoneNumber"] = phoneNumber;
        query["otp"] = otp;

        var requestUrl = $"{settings.Url}?{query}";

        var client = httpClientFactory.CreateClient(HttpClientName);

        logger.LogInformation(
            "Dispatching OTP via webhook GET for phone {Phone}.", phoneNumber);

        var response = await client.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Webhook returned non-success status {Status} for phone {Phone}.",
                (int)response.StatusCode, phoneNumber);

            response.EnsureSuccessStatusCode();
        }

        logger.LogInformation(
            "Webhook GET dispatch succeeded for phone {Phone}.", phoneNumber);
    }
}