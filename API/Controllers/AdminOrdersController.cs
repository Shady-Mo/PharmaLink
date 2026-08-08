namespace API.Controllers;

using Application.DTOs.Order.Requests;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.PrescriptionReviewTeam}")]
public class AdminOrdersController(
    IOrderSplittingService orderSplittingService,
    IOrderService orderService) : BaseApiController
{
    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedList<AdminOrderDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrders([FromQuery] GetOrdersRequest request, CancellationToken cancellationToken)
    {
        var result = await orderService.GetAdminOrders(request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportOrders([FromQuery] ExportOrdersRequest request, CancellationToken cancellationToken)
    {
        var result = await orderService.ExportOrdersForAdmin(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(AdminOrderDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.GetAdminOrderDetail(orderId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{orderId}/resplit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResplitOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await orderSplittingService.ResplitOrderAsync(orderId, User.GetUserId(), cancellationToken);

        return result.IsSuccess
            ? Ok(new { message = "Order re-split completed successfully." })
            : result.ToProblem();
    }

    [HttpPost("{orderId}/approve-prescription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePrescription(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.ApproveOrderPrescription(orderId, cancellationToken);
        return result.IsSuccess ? Ok(new { message = result.Value }) : result.ToProblem();
    }

    [HttpPost("{orderId}/reject-prescription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPrescription(Guid orderId, [FromBody] RejectOrderPrescriptionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required." });
        }

        var result = await orderService.RejectOrderPrescription(orderId, request.Reason, cancellationToken);
        return result.IsSuccess ? Ok(new { message = result.Value }) : result.ToProblem();
    }
}