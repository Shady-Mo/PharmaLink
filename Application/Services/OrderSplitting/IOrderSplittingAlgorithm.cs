using Application.Services.OrderSplitting.Models;

namespace Application.Services.OrderSplitting;

/// <summary>
/// Pluggable strategy for deciding which branch fulfills each order item.
/// Implementations must be pure (no I/O). All data is pre-loaded by the orchestrator.
/// </summary>
public interface IOrderSplittingAlgorithm
{
    string AlgorithmName { get; }
    SplittingResult Execute(SplittingContext context);
}
