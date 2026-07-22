namespace API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
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
}