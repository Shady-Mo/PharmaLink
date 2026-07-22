using Application.DTOs.OrderFulfillmentLeg.Requests;
using Application.DTOs.OrderFulfillmentLeg.Responses;

namespace API.Controllers;

[Authorize]
public class OrderFulfillmentLegsController(IOrderFulfillmentLegService legService) : BaseApiController
{
    /// <summary>
    /// Gets a fulfillment leg status visible to admins, branch-scoped pharmacists, or the owning patient.
    /// </summary>
    [HttpGet("{legId}")]
    [ProducesResponseType(typeof(OrderFulfillmentLegDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid legId, CancellationToken cancellationToken)
    {
        var result = await legService.GetByIdAsync(legId, User, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Updates a fulfillment leg status. Pharmacists are branch-scoped; admins must provide an override reason.
    /// </summary>
    [HttpPatch("{legId}/status")]
    [ProducesResponseType(typeof(OrderFulfillmentLegDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid legId,
        [FromBody] UpdateLegStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await legService.UpdateStatusAsync(legId, request, User, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    /// <summary>
    /// Retrieves All assigned orders for a pharmacist.
    /// </summary>
    [HttpGet("assigned")]
    public async Task<IActionResult> GetAssignedOrders(
        [FromQuery] GetBranchOrdersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await legService.GetBranchOrdersAsync(request, User, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return result.ToProblem();
    }

    /// <summary>
    /// Retrieves detailed information about an assigned order for a pharmacist.
    /// </summary>
    [HttpGet("/api/v1/PharmacistOrders/{id}")]
    [ProducesResponseType(typeof(PharmacistOrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPharmacistOrderDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await legService.GetPharmacistOrderDetailsAsync(id, User, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
