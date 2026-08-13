using Application.DTOs.PharmacyOwner.Request;
using Application.DTOs.PharmacyOwner.Responses;

namespace API.Controllers
{
    /// <summary>
    /// Controller for System Administrators to manage Pharmacy Owners (create, update, soft-delete, list, and pharmacy owner assignment).
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class PharmacyOwnersController(IPharmacyOwnerService pharmacyOwnerService) : BaseApiController
    {
        /// <summary>
        /// Registers a new Pharmacy Owner user account in the system and assigns them the appropriate identity roles.
        /// </summary>
        /// <param name="dto">The signup profile fields and credentials of the new owner user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The profile data of the newly created owner account.</returns>
        /// <response code="201">Owner user created successfully.</response>
        /// <response code="400">If validation fails or email/phone is already in use.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        [HttpPost]
        [ProducesResponseType(typeof(PharmacyOwnerResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePharmacyOwner(
            [FromBody] CreatePharmacyOwnerDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.CreatePharmacyOwnerAsync(dto, cancellationToken);
            if (!result.IsSuccess)
                return result.ToProblem();

            return CreatedAtAction(
                actionName: nameof(GetPharmacyOwner),
                routeValues: new { id = result.Value?.Id },
                value: result.Value
            );
        }

        /// <summary>
        /// Retrieves the profile details of an individual Pharmacy Owner account by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the Pharmacy Owner user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The profile details and associated pharmacy information.</returns>
        /// <response code="200">Owner details retrieved successfully.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the owner user is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PharmacyOwnerResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPharmacyOwner(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.GetPharmacyOwnerByIdAsync(id, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        /// <summary>
        /// Retrieves a paginated, filterable list of all Pharmacy Owners in the system.
        /// </summary>
        /// <param name="request">Pagination, name/email search, status filter, and pharmacy ID assignment filters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paginated list of matching pharmacy owners.</returns>
        /// <response code="200">List retrieved successfully.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<PharmacyOwnerResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPharmacyOwners(
            [FromQuery] GetPharmacyOwnersRequest request,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.GetAllPharmacyOwnersAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        /// <summary>
        /// Updates the profile settings, credentials, associated pharmacy, and status of an existing Pharmacy Owner account.
        /// </summary>
        /// <param name="id">The identifier of the user account to update.</param>
        /// <param name="dto">The updated settings profile.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="400">If validation fails or new email/phone is already used.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the owner user is not found.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePharmacyOwner(
            Guid id,
            [FromBody] UpdatePharmacyOwnerDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.UpdatePharmacyOwnerAsync(id, dto, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        /// <summary>
        /// Soft-deletes a Pharmacy Owner account by updating their status to Inactive.
        /// </summary>
        /// <param name="id">The identifier of the owner account to soft-delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the owner user is not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoftDeletePharmacyOwner(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.SoftDeletePharmacyOwnerAsync(id, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        /// <summary>
        /// Updates the status (Active, Inactive, Suspended) of a Pharmacy Owner account.
        /// </summary>
        /// <param name="id">The identifier of the owner account to update.</param>
        /// <param name="status">The new status value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the owner user is not found.</response>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePharmacyOwnerStatus(
            Guid id,
            [FromBody] UserStatus status,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.ChangePharmacyOwnerStatusAsync(id, status, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        /// <summary>
        /// Assigns this Pharmacy Owner as the super admin/owner of a specific pharmacy.
        /// </summary>
        /// <param name="id">The unique identifier of the Pharmacy Owner user.</param>
        /// <param name="pharmacyId">The unique identifier of the target pharmacy.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="400">If the target user is not registered as a Pharmacy Admin.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If either the user or the pharmacy is not found.</response>
        [HttpPost("{id}/assign-pharmacy/{pharmacyId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignOwner(
            Guid id,
            Guid pharmacyId,
            CancellationToken cancellationToken)
        {
            var result = await pharmacyOwnerService.AssignOwnerAsync(id, pharmacyId, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}
