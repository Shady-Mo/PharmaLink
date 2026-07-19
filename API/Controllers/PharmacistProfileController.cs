namespace API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PharmacistProfileController(IPharmacistProfileService PharmacistProfileService) : BaseApiController
    {

        [HttpGet("")]
        [ProducesResponseType(typeof(GetPharmacyProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = $"{AppRoles.Pharmacist}")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await PharmacistProfileService.GetByIdAsync(userId, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = $"{AppRoles.Pharmacist}")]
        [HttpPut("")]
        [ProducesResponseType(typeof(UpdatePharmacyProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(UpdatePharmacistProfileRequestDTO dto, CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await PharmacistProfileService.UpdateAsync(userId, dto, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }
    }
}
