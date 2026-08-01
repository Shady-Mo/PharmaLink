using Application.Services.PatientCatalog;

namespace API.Controllers;

/// <summary>
/// Patient-facing endpoints for discovering nearby pharmacy branches.
/// </summary>
[Route("api/v1/patient/pharmacies")]
[ApiController]
[Authorize(Roles = AppRoles.Patient)]
public class PatientPharmaciesController(IPatientPharmacyService patientPharmacyService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of verified pharmacy branches sorted by distance from the patient.
    /// </summary>
    /// <param name="request">Geolocation query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Paginated list of nearby branches.</response>
    /// <response code="400">Invalid latitude/longitude values.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PaginatedList<NearbyPharmacyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] NearbyPharmaciesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await patientPharmacyService
            .GetNearbyPharmaciesAsync(request, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
