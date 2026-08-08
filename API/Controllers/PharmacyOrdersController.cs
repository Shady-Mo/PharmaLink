using Application.Services.Order;

namespace API.Controllers;

[Authorize(Roles = AppRoles.PharmacyAdmin)]
[Route("api/v1/pharmacy/orders")]
[ApiController]
public class PharmacyOrdersController(IPharmacyOrderService pharmacyOrderService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated, searchable, filterable, and sortable list of orders assigned to the
    /// authenticated owner's pharmacy.
    /// </summary>
    /// <response code="200">Paginated list of orders.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="403">The authenticated user has no pharmacy context.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PharmacyOrderSummaryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderQueryParametersDto query,
        CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyOrderErrors.PharmacyContextMissing).ToProblem();

        var result = await pharmacyOrderService.GetOrdersAsync(pharmacyId.Value, query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a paginated, searchable, filterable, and sortable list of orders assigned to specific branch
    /// </summary>
    /// <response code="200">Paginated list of orders.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="403">The authenticated user has no pharmacy context.</response>
    [HttpGet("branch/{branchId:guid}")]
    [ProducesResponseType(typeof(PaginatedList<PharmacyOrderSummaryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrdersByBranch(
        Guid branchId,
        [FromQuery] OrderQueryParametersDto query,
        CancellationToken cancellationToken) {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyOrderErrors.PharmacyContextMissing).ToProblem();

        query.BranchId = branchId;

        var result = await pharmacyOrderService.GetOrdersAsync(pharmacyId.Value, query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves complete details for a specific order, only if it belongs to the owner's pharmacy.
    /// </summary>
    /// <response code="200">Full order details.</response>
    /// <response code="403">The authenticated user has no pharmacy context.</response>
    /// <response code="404">The order does not exist or is not assigned to the owner's pharmacy.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyOrderDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyOrderErrors.PharmacyContextMissing).ToProblem();

        var result = await pharmacyOrderService.GetOrderByIdAsync(pharmacyId.Value, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
