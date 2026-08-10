using Application.DTOs.PharmacyAdmin.Request;
using Application.DTOs.PharmacyAdmin.Response;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PharmacyAdminProfile(IPharmacyAdminService pharmacyAdminService) : BaseApiController
    {
        [HttpGet("")]
        [Authorize(Roles = AppRoles.PharmacyAdmin)]
        [ProducesResponseType(typeof(GetPharmacyAdminProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPharmacist()
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await pharmacyAdminService.GetPharmacyAdminProfile(userId);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = $"{AppRoles.PharmacyAdmin}")]
        [HttpPut("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(UpdatePharmacyAdminProfileDTO dto, CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await pharmacyAdminService.UpdateAsync(userId, dto, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }
    }
}
