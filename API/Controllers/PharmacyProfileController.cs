using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;

namespace API.Controllers
{
    [Route("api/v1/pharmacy/profile")]
    [Authorize(Roles = AppRoles.PharmacyAdmin)]
    public class PharmacyProfileController(IPharmacyProfileService pharmacyProfileService) : BaseApiController
    {
        /// <summary>
        /// Retrieves the current pharmacy profile for the authenticated owner's pharmacy.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The pharmacy profile details including read-only fields.</returns>
        /// <response code="200">Profile retrieved successfully.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden — no pharmacy context in JWT.</response>
        /// <response code="404">Pharmacy not found.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var pharmacyId = User.GetPharmacyId();
            if (pharmacyId is null)
                return Result.Failure(DashboardErrors.PharmacyContextMissing).ToProblem();

            var result = await pharmacyProfileService.GetProfileAsync(pharmacyId.Value, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        /// <summary>
        /// Updates editable pharmacy profile fields for the authenticated owner's pharmacy.
        /// Accepts multipart/form-data to support logo image upload.
        /// LicenseNumber is immutable and is not accepted in the request payload.
        /// </summary>
        /// <param name="dto">The editable profile fields including an optional logo file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated pharmacy profile.</returns>
        /// <response code="200">Profile updated successfully.</response>
        /// <response code="400">Validation failed or invalid file upload.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden — no pharmacy context in JWT.</response>
        /// <response code="404">Pharmacy not found.</response>
        [HttpPut]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile(
            [FromForm] UpdatePharmacyProfileDto dto,
            CancellationToken cancellationToken)
        {
            var pharmacyId = User.GetPharmacyId();
            if (pharmacyId is null)
                return Result.Failure(DashboardErrors.PharmacyContextMissing).ToProblem();

            var result = await pharmacyProfileService.UpdateProfileAsync(
                pharmacyId.Value, dto, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
