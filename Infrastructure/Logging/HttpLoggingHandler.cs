namespace Infrastructure.Logging;

public class HttpLoggingHandler(ILogger<HttpLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            // HACK: Semantic Kernel's Gemini connector has a bug where it sends "role":"function"
            // for tool responses, but the Gemini API expects "role":"user" (or "tool" in some SDKs).
            // This causes a 400 Bad Request. We intercept and fix the JSON payload here.
            if (requestBody.Contains("\"role\":\"function\""))
            {
                requestBody = requestBody.Replace("\"role\":\"function\"", "\"role\":\"user\"");

                // We must rebuild the HttpContent with the modified JSON
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            logger.LogWarning("HTTP REQUEST to {Url}: {Body}", request.RequestUri, requestBody);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode) return response;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogError("HTTP RESPONSE {StatusCode} from {Url}: {Body}", response.StatusCode,
            request.RequestUri, responseBody);

        return response;
    }
}