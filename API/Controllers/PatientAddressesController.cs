using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;

namespace API.Controllers;

public class PatientAddressesController(
    IAddressService addressService) : BaseApiController
{
    /// <summary>Creates a new delivery address for the authenticated Patient.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(AddressResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddressRequestDTO request, CancellationToken cancellationToken)
    {
        var result = await addressService.CreateAsync(User.GetUserId(), request, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.AddressId }, result.Value);
    }

    /// <summary>Lists all delivery addresses belonging to the authenticated Patient.</summary>
    [HttpGet("MyAddresses")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(List<AddressResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllByPatient(CancellationToken cancellationToken)
    {
        var result = await addressService.GetAllForPatientAsync(User.GetUserId(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Lists all Addresses by Admin </summary>
    [HttpGet()]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(List<AddressResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllByAdmin(CancellationToken cancellationToken)
    {
        var result = await addressService.GetAllAddressesByAdminAsync( cancellationToken);
        return Ok(result.Value);
    }


    /// <summary>
    /// Gets a single address. Patients may only fetch their own address (403 otherwise).
    /// </summary>
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Admin}")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AddressResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id, CancellationToken cancellationToken)
    {
       
        
            var result = await addressService.GetByIdAsync(id, User.GetUserId(), User.GetRoleName(),cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        
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
        var result = await addressService.UpdateAsync(id, User.GetUserId(), request, cancellationToken);

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
        var result = await addressService.DeleteAsync(id, User.GetUserId(), cancellationToken);

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
        var result = await addressService.SetDefaultAsync(id, User.GetUserId(), cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return NoContent();
    }
}