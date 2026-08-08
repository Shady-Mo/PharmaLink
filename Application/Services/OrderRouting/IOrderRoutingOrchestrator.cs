using Application.DTOs.OrderRouting;

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
}
