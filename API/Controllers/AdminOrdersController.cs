namespace API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminOrdersController(
    IOrderSplittingService orderSplittingService,
    IOrderService orderService) : BaseApiController
{
    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedList<GetOrderDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrders([FromQuery] GetOrdersRequest request)
    {
        var result = await orderService.GetOrdersForAdmin(request);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(GetOrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var result = await orderService.GetOrderForAdmin(orderId);
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