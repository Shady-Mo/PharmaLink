using Microsoft.SemanticKernel;
using System.Net;

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
            Polly.CircuitBreaker.BrokenCircuitException => true, // When circuit breaker opens, we must fallback to the next provider
            HttpOperationException httpEx => IsTransientStatusCode(httpEx.StatusCode),
            System.ClientModel.ClientResultException clientEx => IsTransientStatusCode((HttpStatusCode)clientEx.Status),
            InvalidOperationException invEx when invEx.Message.Contains("status", StringComparison.OrdinalIgnoreCase) => true, // API failures encapsulated in InvalidOperationException
            ArgumentOutOfRangeException argEx when argEx.Message.Contains("ChatFinishReason", StringComparison.OrdinalIgnoreCase) => true, // OpenAI SDK doesn't support 'error' finish reason from OpenRouter
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
