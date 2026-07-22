namespace API.Controllers
{
    /// <summary>
    /// Controller for System Administrators to manage pharmacies in the system, including approvals, status updates, and detail lookups.
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminPharmaciesDashboardController(IAdminPharmacyService adminPharmacyService) : BaseApiController
    {
        /// <summary>
        /// Retrieves a paginated list of all pharmacies filtered by name search, verification status, and branch location city.
        /// </summary>
        /// <param name="request">Pagination and search/filtering parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paginated list of pharmacies including branch and drug catalog counts, and owner info.</returns>
        /// <response code="200">List of pharmacies retrieved successfully.</response>
        /// <response code="401">Unauthorized if the user token is missing or invalid.</response>
        /// <response code="403">Forbidden if the user is not a System Administrator.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<AdminPharmacySummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPharmacies(
            [FromQuery] GetAdminPharmaciesRequest request,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.GetAllPharmaciesAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        /// <summary>
        /// Retrieves the detailed profile of a pharmacy by its ID, including all branches and owner admin accounts.
        /// </summary>
        /// <param name="id">The unique identifier of the pharmacy.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Detailed pharmacy profile metrics and lists of branches.</returns>
        /// <response code="200">Pharmacy details retrieved successfully.</response>
        /// <response code="401">Unauthorized if token is invalid.</response>
        /// <response code="403">Forbidden if not System Admin.</response>
        /// <response code="404">If the pharmacy is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AdminPharmacyDetailDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPharmacy(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.GetPharmacyByIdAsync(id, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        /// <summary>
        /// Creates a new pharmacy in the system (e.g. from physical forms or registrations).
        /// </summary>
        /// <param name="dto">The metadata and file fields of the pharmacy to register.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ID of the newly created pharmacy.</returns>
        /// <response code="201">Pharmacy created successfully.</response>
        /// <response code="400">If validation fails or the license number already exists.</response>
        /// <response code="401">Unauthorized if token is invalid.</response>
        /// <response code="403">Forbidden if not System Admin.</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePharmacy(
            [FromForm] AdminCreatePharmacyDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.CreatePharmacyAsync(dto, cancellationToken);
            if (!result.IsSuccess)
                return result.ToProblem();

            return CreatedAtAction(
                actionName: nameof(GetPharmacy),
                routeValues: new { id = result.Value },
                value: new { PharmacyId = result.Value, Message = "Pharmacy created successfully." }
            );
        }

        /// <summary>
        /// Updates the core information (name, license, logo) and status of an existing pharmacy.
        /// </summary>
        /// <param name="id">The unique identifier of the pharmacy to update.</param>
        /// <param name="dto">The updated pharmacy fields.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="244">No Content on successful update.</response>
        /// <response code="400">If validation fails or new license number is already used.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the pharmacy is not found.</response>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePharmacy(
            Guid id,
            [FromForm] AdminUpdatePharmacyDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.UpdatePharmacyAsync(id, dto, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        /// <summary>
        /// Performs a soft delete on a pharmacy by marking its verification status as Deleted.
        /// </summary>
        /// <param name="id">The identifier of the pharmacy to soft-delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the pharmacy is not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoftDeletePharmacy(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.SoftDeletePharmacyAsync(id, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        /// <summary>
        /// Updates the verification status (Pending, Verified, Rejected, Deleted) of a pharmacy.
        /// </summary>
        /// <param name="id">The identifier of the pharmacy to update.</param>
        /// <param name="status">The new verification status value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If the pharmacy is not found.</response>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePharmacyStatus(
            Guid id,
            [FromBody] VerificationStatus status,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.ChangePharmacyStatusAsync(id, status, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        /// <summary>
        /// Assigns a Pharmacy Admin user as the super admin/owner of a pharmacy.
        /// </summary>
        /// <param name="id">The identifier of the pharmacy.</param>
        /// <param name="userId">The user identifier of the Pharmacy Admin.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">No Content on success.</response>
        /// <response code="400">If the target user is not registered as a Pharmacy Admin.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">If either the pharmacy or admin user is not found.</response>
        [HttpPost("{id}/assign-owner/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignOwner(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.AssignOwnerAsync(id, userId, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}
