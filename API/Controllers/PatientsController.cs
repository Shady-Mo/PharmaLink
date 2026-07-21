namespace API.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize(Roles = AppRoles.Patient)]
public class PatientsController(
    IPatientService patientService,
    ILogger<PatientsController> logger) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var userIdStr = User.FindFirst("UserID")?.Value          
                        ?? User.FindFirst("userId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var patientId))
        {
            logger.LogWarning("Unauthorized profile access attempt: Patient Claim 'UserID' not found or invalid.");
            return Unauthorized();
        }

        logger.LogInformation("Authenticated patient {PatientId} requested their profile details.", patientId);

        var result = await patientService.GetProfileAsync(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }


    // -------**-**-*-*-****-*-*-** //

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdStr = User.FindFirst("UserID")?.Value
                        ?? User.FindFirst("userId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var patientId))
        {
            logger.LogWarning("Unauthorized profile update attempt: Invalid token claims.");
            return Unauthorized();
        }

        logger.LogInformation("Authenticated patient {PatientId} requested a profile update.", patientId);

        var result = await patientService.UpdateProfileAsync(patientId, updateDto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }



}