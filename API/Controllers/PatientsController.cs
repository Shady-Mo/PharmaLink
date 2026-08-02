namespace API.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize(Roles = AppRoles.Patient)]
public class PatientsController(
    IPatientService patientService,
    ILogger<PatientsController> logger) : BaseApiController
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var patientId = User.GetUserId();
        if (patientId == Guid.Empty)
        {
            logger.LogWarning("Unauthorized profile access attempt: Patient Claim 'UserID' not found or invalid.");
            return Unauthorized();
        }

        logger.LogInformation("Authenticated patient {PatientId} requested their profile details.", patientId);

        var result = await patientService.GetProfileAsync(patientId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }


    // -------**-**-*-*-****-*-*-** //

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default)
    {

        var patientId = User.GetUserId();
        if (patientId == Guid.Empty)
        {
            logger.LogWarning("Unauthorized profile update attempt: Invalid token claims.");
            return Unauthorized();
        }

        logger.LogInformation("Authenticated patient {PatientId} requested a profile update.", patientId);

        var result = await patientService.UpdateProfileAsync(patientId, updateDto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("profile/picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureDto uploadDto, CancellationToken cancellationToken = default)
    {

        var patientId = User.GetUserId();
        if (patientId == Guid.Empty)
        {
            logger.LogWarning("Unauthorized profile picture upload attempt: Invalid token claims.");
            return Unauthorized();
        }

        var result = await patientService.UploadProfilePictureAsync(patientId, uploadDto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}