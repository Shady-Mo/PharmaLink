using API.Extensions;
using Application.DTOs.Admin.Users;
using Application.DTOs;
using Application.Services;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace API.Controllers
{
    /// <summary>
    /// Controller for System Administrators to manage all users in the system.
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminUsersController(IAdminUserService adminUserService) : BaseApiController
    {
        /// <summary>
        /// Retrieves a paginated and filterable list of all users in the system.
        /// </summary>
        /// <param name="filter">Pagination parameters, search by name/email, role filter, status filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paginated list of matching users.</returns>
        /// <response code="200">List retrieved successfully.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<AdminUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] AdminUserFilterDto filter,
            CancellationToken cancellationToken)
        {
            var result = await adminUserService.GetUsersAsync(filter, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        /// <summary>
        /// Updates the active status of a user in the system.
        /// </summary>
        /// <param name="id">The unique identifier of the user to update.</param>
        /// <param name="dto">The new status value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="400">If the admin attempts to deactivate their own account.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the user is not found.</response>
        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserStatus(
            Guid id,
            [FromBody] UpdateUserStatusDto dto,
            CancellationToken cancellationToken)
        {
            var currentAdminId = User.GetUserId();
            var result = await adminUserService.UpdateUserStatusAsync(id, dto, currentAdminId, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPut("{id}/role")]
        [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRole(
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            CancellationToken cancellationToken)
        {
            var currentAdminId = User.GetUserId();
            var result = await adminUserService.UpdateUserRoleAsync(id, dto, currentAdminId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
