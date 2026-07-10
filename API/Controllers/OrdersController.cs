namespace API.Controllers;

/// <summary>
/// Handles order operations for patients including order creation, retrieval, and listing.
/// </summary>
public class OrdersController(IOrderService orderService) : BaseApiController
{
    /// <summary>
    /// Creates a new order with multiple drugs for the authenticated patient.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - PatientUserID is derived from the authenticated JWT, never from the request body.
    /// - Only Patient role can create orders. Pharmacist and System Admin get 403 Forbidden.
    /// - DeliveryAddressID must belong to the requesting patient.
    /// - Invalid DrugIDs are validated atomically before order creation.
    /// 
    /// **Acceptance Criteria (PHAR-302):**
    /// - Creates ORDERS record with OrderStatus = 0 (Pending).
    /// - Creates ORDER_ITEMS with ItemStatus = 0 (Pending) and BranchID = NULL.
    /// - Total amount is calculated from items' UnitPrice × QuantityNeeded.
    /// </remarks>
    /// <param name="createOrderDTO">Order details including items, delivery address, and fulfillment mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **201 Created** with the new OrderID and initial OrderStatus on success.  
    /// **400 Bad Request** if validation fails (empty items, invalid drugs, invalid address).  
    /// **403 Forbidden** if the user is not a Patient.
    /// </returns>
    [HttpPost("")]
    [Authorize]
    [ProducesResponseType(typeof(OrderCreatedResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderDTO createOrderDTO,
        CancellationToken cancellationToken)
    {
        var patientIdClaim = User.FindFirst(JwtClaimTypes.UserId);
        if (patientIdClaim is null || !Guid.TryParse(patientIdClaim.Value, out var patientId))
            return Unauthorized(new { message = "Invalid or missing user ID in token." });

        var result = await orderService.CreateOrder(patientId, createOrderDTO);

        if (result.IsFailure)
            return result.ToProblem();

        return CreatedAtAction(
            actionName: nameof(GetOrder),
            routeValues: new { id = result.Value?.OrderId },
            value: result.Value);
    }

    /// <summary>
    /// Retrieves a specific order by its ID for the authenticated patient.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - Patients can only retrieve their own orders.
    /// - Cross-patient access attempts return 403 Forbidden.
    /// - System Admin can retrieve any order (future enhancement).
    /// </remarks>
    /// <param name="id">The unique identifier of the order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the order details and items.  
    /// **404 Not Found** if the order does not exist or does not belong to the patient.  
    /// **403 Forbidden** if attempting to access another patient's order.
    /// </returns>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(GetOrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var patientIdClaim = User.FindFirst(JwtClaimTypes.UserId);
        if (patientIdClaim is null || !Guid.TryParse(patientIdClaim.Value, out var patientId))
            return Unauthorized(new { message = "Invalid or missing user ID in token." });

        var result = await orderService.GetOrder(id, patientId);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a paginated list of orders for the authenticated patient.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - Patients only see their own orders.
    /// - Results are scoped to PatientUserID from the JWT.
    /// - Pagination defaults to page 1 with 10 items per page.
    /// 
    /// **Pagination Metadata:**
    /// - PageNumber: Current page requested.
    /// - TotalPages: Total number of pages available.
    /// - HasPreviousPage: Boolean flag indicating if a previous page exists.
    /// - HasNextPage: Boolean flag indicating if a next page exists.
    /// </remarks>
    /// <param name="pageNumber">The page number to retrieve (default: 1). Must be >= 1.</param>
    /// <param name="pageSize">The number of records per page (default: 10). Must be between 1 and 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with paginated order list and metadata.  
    /// **400 Bad Request** if pagination parameters are invalid.
    /// </returns>
    [HttpGet("")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedList<GetOrderDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var patientIdClaim = User.FindFirst(JwtClaimTypes.UserId);
        if (patientIdClaim is null || !Guid.TryParse(patientIdClaim.Value, out var patientId))
            return Unauthorized(new { message = "Invalid or missing user ID in token." });

        // Validate pagination parameters
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "PageNumber must be >= 1, and PageSize must be between 1 and 100." });

        var result = await orderService.GetOrders(patientId, pageNumber, pageSize);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
