using Application.DTOs.OrderRouting;

namespace Application.Services.OrderSplitting;

public interface IOrderSplittingService
{
    /// <summary>
    /// System-triggered: run the greedy consolidation algorithm immediately after order creation.
    /// Sets OrderStatus = Processing, assigns BranchId to each item, reserves stock,
    /// then calls GenerateLegsAsync. Not directly callable by Patient or Pharmacist.
    /// Returns the <see cref="OrderRoutingPlan"/> the engine produced (fulfillment legs with
    /// per-branch distances + Arabic/English drug groups, and any unfulfillable items) so the
    /// order-creation response can surface it to the patient.
    /// </summary>
    Task<Result<OrderRoutingPlan>> SplitOrderAsync(Guid orderId, CancellationToken cancellationToken = default);


    /// <summary>
    /// Admin-triggered: releases existing reservations for Awarded items, resets all
    /// non-Cancelled items to Pending, then re-runs SplitOrderAsync.
    /// Fully audit-logged via ILogger.
    /// </summary>
    Task<Result> ResplitOrderAsync(Guid orderId, Guid adminUserId, CancellationToken cancellationToken = default);
}