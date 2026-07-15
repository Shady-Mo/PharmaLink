using Application.DTOs.Pharmacy.Responses;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PharmacyController(IProfileService profileService) : BaseApiController
    {

        [HttpGet("")]
        [ProducesResponseType(typeof(GetPharmacyProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = $"{AppRoles.Pharmacist}")]
        public async Task<IActionResult> GetDrugById(CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await profileService.GetByIdAsync(userId, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
