using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;

namespace API.Controllers
{
    public class AddressesController(
    IAddressService addressService,
    ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Creates a new delivery address for the authenticated Patient.</summary>
    [Authorize(Roles = AppRoles.Patient)]
    [HttpPost]
    [ProducesResponseType(typeof(AddressResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddressRequestDTO request, CancellationToken cancellationToken)
    {
        var result = await addressService.CreateAsync(currentUser.UserId, request, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.AddressId }, result.Value);
    }

    /// <summary>Lists all delivery addresses belonging to the authenticated Patient.</summary>
    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet]
    [ProducesResponseType(typeof(List<AddressResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await addressService.GetAllForPatientAsync(currentUser.UserId, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a single address. Patients may only fetch their own address (403 otherwise).
    /// System Admins may fetch any address for support purposes but must supply a
    /// <c>reason</c> query parameter; every such read is written to the audit log.
    /// </summary>
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Admin}")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AddressResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var result = await addressService.GetByIdAsync(
            id, currentUser.UserId, currentUser.RoleName!, reason, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

    /// <summary>Updates an address owned by the authenticated Patient.</summary>
    [Authorize(Roles = AppRoles.Patient)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AddressResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAddressRequestDTO request, CancellationToken cancellationToken)
    {
        var result = await addressService.UpdateAsync(id, currentUser.UserId, request, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

    /// <summary>Deletes an address owned by the authenticated Patient.</summary>
    [Authorize(Roles = AppRoles.Patient)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await addressService.DeleteAsync(id, currentUser.UserId, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return NoContent();
    }

    /// <summary>Atomically marks this address as default, unsetting all others for the Patient.</summary>
    [Authorize(Roles = AppRoles.Patient)]
    [HttpPatch("{id:guid}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        var result = await addressService.SetDefaultAsync(id, currentUser.UserId, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return NoContent();
    }
}
}
