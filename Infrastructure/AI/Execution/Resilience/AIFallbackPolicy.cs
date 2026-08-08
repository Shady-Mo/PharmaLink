using System.ClientModel;
using System.Net;
using Microsoft.SemanticKernel;
using Polly.CircuitBreaker;

namespace Infrastructure.AI.Execution.Resilience;

public static class AIFallbackPolicy
{
    /// <summary>
    /// Determines whether an exception is a transient error that warrants a retry or fallback.
    /// </summary>
    public static bool IsTransient(Exception ex)
    {
        return ex switch
        {
            TaskCanceledException => true, // Timeout
            HttpRequestException => true, // Network failures
            BrokenCircuitException => true, // When circuit breaker opens, we must fall back to the next provider
            HttpOperationException httpEx => IsTransientStatusCode(httpEx.StatusCode),
            ClientResultException clientEx => IsTransientStatusCode((HttpStatusCode)clientEx.Status),
            InvalidOperationException invEx when invEx.Message.Contains("status", StringComparison.OrdinalIgnoreCase) =>
                true, // API failures encapsulated in InvalidOperationException
            ArgumentOutOfRangeException argEx when argEx.Message.Contains("ChatFinishReason",
                    StringComparison.OrdinalIgnoreCase) =>
                true, // OpenAI SDK doesn't support 'error' finish reason from OpenRouter
            _ => false
        };
    }

    private static bool IsTransientStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode == null) return true;

        return statusCode switch
        {
            HttpStatusCode.TooManyRequests => true, // 429
            HttpStatusCode.RequestTimeout => true, // 408
            HttpStatusCode.InternalServerError => true, // 500
            HttpStatusCode.BadGateway => true, // 502
            HttpStatusCode.ServiceUnavailable => true, // 503
            HttpStatusCode.GatewayTimeout => true, // 504
            _ => false
        };
    }
}