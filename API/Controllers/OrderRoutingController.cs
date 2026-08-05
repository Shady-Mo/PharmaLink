using Application.DTOs.OrderRouting;
using Application.Services.OrderRouting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Test and preview endpoints for the Semantic Kernel Multi-Agent Order Fulfillment Engine.
/// </summary>
[Route("api/v1/order-routing")]
[ApiController]
public class OrderRoutingController(IOrderRoutingOrchestrator orchestrator) : ControllerBase
{
    /// <summary>
    /// **TEST ENDPOINT** — Runs the multi-agent routing engine over a patient's cart to preview
    /// the optimal fulfillment plan (strategy, legs, distances, unfulfillable items) WITHOUT
    /// placing an order.
    /// </summary>
    /// <remarks>
    /// Use this to test the AI engine's decisions before integrating with the checkout flow.
    /// 
    /// **Security:**
    /// - Patient JWT required — patient ID derived from the JWT.
    /// - Cart items come from the patient's active cart (not the body).
    /// 
    /// **What it returns:**
    /// - `strategy`: "SinglePharmacy" or "MultiBranchSplit"
    /// - `legs`: per-branch allocation (branch name, distance, items, subtotal)
    /// - `unfulfillableItems`: items no nearby branch can supply
    /// - `totalDistanceKm`: sum of all leg distances
    /// - `reasoning`: the AI's explanation (or deterministic fallback reason if the agent failed)
    /// 
    /// **Backend flow:**
    /// `OptimizeOrderFulfillmentAsync` → PharmacyInventoryPlugin (evaluates nearby branches) →
    /// Multi-agent chat (InventoryCheckAgent + RouteOptimizationAgent) → plan reconciliation
    /// (validates GUIDs/prices/distances against real data).
    /// </remarks>
    /// <param name="request">Patient location + cart items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the routing plan.  
    /// **400 Bad Request** if the cart is empty or location is invalid.  
    /// **403 Forbidden** if not a Patient.
    /// </returns>
    [HttpPost("preview")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(OrderRoutingPlan), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderRoutingPlan>> PreviewRouting(
        [FromBody] OrderRoutingPreviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CartItems is null || request.CartItems.Count == 0)
            return BadRequest(new { error = "Cart is empty." });

        if (request.PatientLocation is null)
            return BadRequest(new { error = "Patient location is required." });

        var plan = await orchestrator.OptimizeOrderFulfillmentAsync(
            User.GetUserId(),
            request.PatientLocation,
            request.CartItems,
            cancellationToken);

        return Ok(plan);
    }
}

/// <summary>
/// Request model for the routing preview endpoint.
/// </summary>
public record OrderRoutingPreviewRequest
{
    /// <summary>Patient's delivery location (latitude/longitude).</summary>
    public GeoLocation PatientLocation { get; init; } = default!;

    /// <summary>Cart items to route (DrugId + Quantity).</summary>
    public IReadOnlyList<CartItemDto> CartItems { get; init; } = [];
}
