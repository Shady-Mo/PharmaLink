using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PharmaciestController(IProfileService profileService) : BaseApiController
    {

        [HttpGet("")]
        [ProducesResponseType(typeof(GetPharmacyProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = $"{AppRoles.Pharmacist}")]
        public async Task<IActionResult> GetById(CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await profileService.GetByIdAsync(userId, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = $"{AppRoles.Pharmacist}")]
        [HttpPut("")]
        [ProducesResponseType(typeof(UpdatePharmacyProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(UpdatePharmacyProfileRequestDTO dto, CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await profileService.UpdateAsync(userId, dto, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }
    }
}
