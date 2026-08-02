namespace Infrastructure.AI.Execution.Resilience;

public enum CircuitBreakerState
{
    Healthy,
    Degraded,
    RateLimited,
    Unavailable,
    OpenCircuit,
    HalfOpen,
    Disabled
}

public interface ICircuitBreakerTracker
{
    CircuitBreakerState GetState(string providerName, string modelId);
    void RecordSuccess(string providerName, string modelId);
    void RecordFailure(string providerName, string modelId, Exception exception);
    bool CanExecute(string providerName, string modelId);
}
