using Application.DTOs.OrderRouting;
using Application.Services.OrderSplitting.Models;

namespace Application.Services.OrderRouting;

/// <summary>
/// Order Fulfillment Optimization Engine.
///
/// Orchestrates a Semantic Kernel multi-agent interaction (Inventory &amp; Distance worker
/// agent → Route Optimization decision agent) to evaluate a patient's cart against nearby
/// pharmacy inventories and distances, then produce an optimal fulfillment plan that
/// minimizes the number of fulfilling pharmacies first, and travel distance second.
/// </summary>
public interface IOrderRoutingOrchestrator
{
    /// <summary>
    /// Runs the agent interaction flow and returns the optimal fulfillment plan.
    /// Standalone entry point (e.g. cart preview) that queries live inventory itself.
    /// </summary>
    /// <param name="patientUserId">The authenticated patient placing the order.</param>
    /// <param name="patientLocation">The patient's delivery coordinates.</param>
    /// <param name="cartItems">The cart lines to route.</param>
    /// <param name="fulfillmentMode">
    /// Delivery vs Pickup — controls the geographic candidate filter: Delivery keeps branches whose
    /// ServiceRadiusKm covers the patient; Pickup keeps branches within a fixed 20 km drive.
    /// </param>
    Task<OrderRoutingPlan> OptimizeOrderFulfillmentAsync(
        Guid patientUserId,
        GeoLocation patientLocation,
        IReadOnlyList<CartItemDto> cartItems,
        FulfillmentMode fulfillmentMode,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Pipeline integration entry point used during Cart-to-Order conversion.
    ///
    /// Consumes the <see cref="SplittingContext"/> already assembled by the
    /// <c>OrderSplittingService</c> (candidate branches filtered by geo-radius + fulfillment
    /// mode, inventory snapshot filtered by expiry) and lets the multi-agent engine decide
    /// the item→branch allocation. Returning the same <see cref="SplittingResult"/> shape as
    /// the deterministic algorithm keeps all downstream reservation/leg-generation logic
    /// untouched.
    /// </summary>
    /// <returns>
    /// The agent-decided assignments, or <c>null</c> if the engine could not produce a usable
    /// decision (the caller should then fall back to the deterministic algorithm).
    /// </returns>
    Task<SplittingResult?> OptimizeSplitAsync(
        SplittingContext context,
        CancellationToken cancellationToken = default);
}
