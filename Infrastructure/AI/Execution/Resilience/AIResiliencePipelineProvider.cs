using System.Collections.Concurrent;
using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Infrastructure.AI.Execution.Resilience;

public interface IAIResiliencePipelineProvider
{
    ResiliencePipeline GetPipeline(ProviderModelTarget target);
    CircuitBreakerState GetCircuitState(ProviderModelTarget target);
}

public class AIResiliencePipelineProvider : IAIResiliencePipelineProvider
{
    private readonly AiOptions _options;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new();
    private readonly ConcurrentDictionary<string, CircuitBreakerStateProvider> _cbStates = new();

    public AIResiliencePipelineProvider(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public ResiliencePipeline GetPipeline(ProviderModelTarget target)
    {
        var key = $"{target.ProviderName}:{target.ModelId}";

        return _pipelines.GetOrAdd(key, _ =>
        {
            var cbStateProvider = new CircuitBreakerStateProvider();
            _cbStates[key] = cbStateProvider;

            var retryConfig = _options.RetryPolicy;
            var cbConfig = _options.CircuitBreaker;

            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(AIFallbackPolicy.IsTransient),
                    MaxRetryAttempts = retryConfig.RetryCount,
                    Delay = TimeSpan.FromMilliseconds(retryConfig.InitialDelayMs),
                    MaxDelay = TimeSpan.FromMilliseconds(retryConfig.MaxDelayMs),
                    BackoffType = DelayBackoffType.Exponential
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(AIFallbackPolicy.IsTransient),
                    FailureRatio = 0.5,
                    MinimumThroughput = cbConfig.FailureThreshold,
                    BreakDuration = TimeSpan.FromSeconds(cbConfig.CooldownSeconds),
                    StateProvider = cbStateProvider
                })
                .Build();
        });
    }

    public CircuitBreakerState GetCircuitState(ProviderModelTarget target)
    {
        var key = $"{target.ProviderName}:{target.ModelId}";
        if (_cbStates.TryGetValue(key, out var stateProvider))
        {
            return stateProvider.CircuitState switch
            {
                CircuitState.Open => CircuitBreakerState.OpenCircuit,
                CircuitState.HalfOpen => CircuitBreakerState.HalfOpen,
                CircuitState.Closed => CircuitBreakerState.Healthy,
                CircuitState.Isolated => CircuitBreakerState.Disabled,
                _ => CircuitBreakerState.Healthy
            };
        }

        return CircuitBreakerState.Healthy;
    }
}