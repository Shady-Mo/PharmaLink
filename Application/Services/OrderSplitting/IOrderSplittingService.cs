namespace Application.Services.OrderSplitting;

public interface IOrderSplittingService
{
    /// <summary>
    /// System-triggered: run the greedy consolidation algorithm immediately after order creation.
    /// Sets OrderStatus = Processing, assigns BranchId to each item, reserves stock,
    /// then calls GenerateLegsAsync. Not directly callable by Patient or Pharmacist.
    /// </summary>
    Task<Result> SplitOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin-triggered: releases existing reservations for Awarded items, resets all
    /// non-Cancelled items to Pending, then re-runs SplitOrderAsync.
    /// Fully audit-logged via ILogger.
    /// </summary>
    Task<Result> ResplitOrderAsync(Guid orderId, Guid adminUserId, CancellationToken cancellationToken = default);
}