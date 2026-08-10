using Application.DTOs.Admin;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/v1/admin/profile")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
        public class AdminProfileController(IAdminService adminService) : ControllerBase
        {
            /// <summary>
            /// Retrieves the profile details for the authenticated administrator.
            /// </summary>
            [HttpGet]
            [ProducesResponseType(typeof(AdminProfileResponseDto), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
            {
                var userId = User.FindFirstValue(JwtClaimTypes.UserId);

                if (!Guid.TryParse(userId, out var parsedAdminId))
                    return Unauthorized("Invalid or missing User ID in token.");

                var result = await adminService.GetProfileAsync(parsedAdminId, cancellationToken);

                return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
            }

            /// <summary>
            /// Updates profile details for the authenticated administrator.
            /// </summary>
            [HttpPut]
            [ProducesResponseType(typeof(AdminProfileResponseDto), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status409Conflict)]
            public async Task<IActionResult> UpdateProfile(
                [FromBody] UpdateAdminProfileDto updateDto,
                CancellationToken cancellationToken)
            {
                var userId = User.FindFirstValue(JwtClaimTypes.UserId);

                if (!Guid.TryParse(userId, out var parsedAdminId))
                    return Unauthorized("Invalid or missing User ID in token.");

                var result = await adminService.UpdateProfileAsync(parsedAdminId, updateDto, cancellationToken);

                return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
            }
        }
    }

