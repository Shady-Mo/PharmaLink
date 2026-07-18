namespace API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = AppRoles.PharmacyAdmin)]
public class PharmacistsController(IPharmacistManagementService pharmacistService) : BaseApiController
{
    /// <summary>Creates a new pharmacist account.</summary>
    /// <response code="201">Pharmacist created successfully. Returns the full profile.</response>
    /// <response code="400">Validation error in request body.</response>
    /// <response code="403">Caller is not a System Admin.</response>
    /// <response code="409">Email or phone number is already registered.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PharmacistResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePharmacist(
        [FromBody] CreatePharmacistRequestDTO dto,
        CancellationToken cancellationToken)
    {
        var result = await pharmacistService.CreatePharmacistAsync(User.GetUserId(), dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetPharmacist),
                routeValues: new { id = result.Value!.PharmacistId },
                value: result.Value)
            : result.ToProblem();
    }

    /// <summary>Retrieves a paginated list of all pharmacists.</summary>
    /// <response code="200">Success. Returns a paginated list of pharmacist summaries.</response>
    /// <response code="403">Caller is not a System Admin.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PharmacistSummaryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPharmacists(
        [FromQuery] PaginatedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacistService.GetAllPharmacistsAsync(User.GetUserId(), request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>Retrieves a specific pharmacist including full assignment history.</summary>
    /// <response code="200">Success. Returns the full pharmacist profile with history array.</response>
    /// <response code="403">Caller is not a System Admin.</response>
    /// <response code="404">Pharmacist with the given ID was not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacistResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPharmacist(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await pharmacistService.GetPharmacistByIdAsync(User.GetUserId(), id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>Updates pharmacist profile fields (FullName, PhoneNumber).</summary>
    /// <response code="200">Success. Returns the updated pharmacist profile.</response>
    /// <response code="400">Validation error in request body.</response>
    /// <response code="403">Caller is not a System Admin.</response>
    /// <response code="404">Pharmacist with the given ID was not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PharmacistResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePharmacist(
        Guid id,
        [FromBody] UpdatePharmacistRequestDTO dto,
        CancellationToken cancellationToken)
    {
        var result = await pharmacistService.UpdatePharmacistAsync(User.GetUserId(), id, dto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Deletes a pharmacist account. Assignment history rows are preserved for auditing.
    /// </summary>
    /// <response code="204">Pharmacist account deleted. No content returned.</response>
    /// <response code="403">Caller is not a System Admin.</response>
    /// <response code="404">Pharmacist with the given ID was not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePharmacist(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await pharmacistService.DeletePharmacistAsync(User.GetUserId(), id, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    /// Retrieves the assignment history of a specific pharmacist.
    /// </summary>
    /// <response code="200">Success. Returns a list of assignment history items.</response>
    /// <response code="403">Caller is not a Pharmacy Admin or Admin not assigned to pharmacy.</response>
    /// <response code="404">Pharmacist not found or not employed at this pharmacy.</response>
    [HttpGet("/api/v1/pharmacisthistory/{id:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentHistoryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPharmacistHistory(
        Guid id,
        CancellationToken cancellationToken) {
        var result = await pharmacistService.GetPharmacistHistoryAsync(User.GetUserId(), id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
