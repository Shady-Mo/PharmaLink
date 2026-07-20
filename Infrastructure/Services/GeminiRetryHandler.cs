using System.Net;

namespace Infrastructure.Services;

public class GeminiRetryHandler(ILogger<GeminiRetryHandler> logger, int maxRetries = 3) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (true)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode || (response.StatusCode != HttpStatusCode.ServiceUnavailable &&
                                                 response.StatusCode != HttpStatusCode.TooManyRequests) ||
                retryCount >= maxRetries)
            {
                return response;
            }

            retryCount++;
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));

            logger.LogWarning(
                "Gemini API returned {StatusCode}. Retrying in {Delay}s... (Attempt {RetryCount} of {MaxRetries})",
                response.StatusCode, delay.TotalSeconds, retryCount, maxRetries);

            await Task.Delay(delay, cancellationToken);
        }
    }
}