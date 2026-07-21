using Twilio.TwiML.Voice;

namespace API.Controllers;

/// <summary>
/// Handles prescription review operations including upload, retrieval, updating, approval, and rejection.
/// </summary>
public class PrescriptionReviewsController(
    IPrescriptionReviewService service) : BaseApiController
{
    /// <summary>
    /// Uploads a prescription image and performs AI extraction on the medicines.
    /// </summary>
    /// <param name="dto">The multipart form data request containing the image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created response containing the initial AI extraction details.</returns>
    [HttpPost("")]
    [Authorize(Roles = AppRoles.Patient)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PrescriptionReviewUploadResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadAndExtract(
        [FromForm] UploadPrescriptionDTO dto,
        CancellationToken cancellationToken)
    {
        var result = await service.UploadAndExtractAsync(User.GetUserId(), dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(
                actionName: nameof(GetPrescriptionReview),
                routeValues: new { id = result.Value?.ReviewId },
                value: result.Value)
            : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a paginated list of prescription reviews. Only accessible by Pharmacists and Admins.
    /// </summary>
    [HttpGet("")]
    [Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(PaginatedList<PrescriptionReviewSummaryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPrescriptionReviews(
        [FromQuery] GetPrescriptionReviewsRequest request)
    {
        var result = await service.GetAllAsync(request);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a specific prescription review by ID. Accessible by both the Patient (owner only) and Pharmacist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Pharmacist},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(PrescriptionReviewDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrescriptionReview(
        Guid id)
    {
        var role = User.GetRoleName() ?? string.Empty;
        var result = await service.GetByIdAsync(id, User.GetUserId(), role);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Updates the extracted medicines list for a pending review. Only accessible by Pharmacists.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Pharmacist)]
    [ProducesResponseType(typeof(PrescriptionReviewDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMedicines(
        Guid id,
        [FromBody] UpdatePrescriptionReviewDTO dto)
    {
        var result = await service.UpdateMedicinesAsync(id, User.GetUserId(), dto);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Approves a prescription review. Only accessible by Pharmacists.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = AppRoles.Pharmacist)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveRejectDTO dto)
    {
        var result = await service.ApproveAsync(id, User.GetUserId(), dto);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    /// Rejects a prescription review. Only accessible by Pharmacists.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = AppRoles.Pharmacist)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] ApproveRejectDTO dto)
    {
        var result = await service.RejectAsync(id, User.GetUserId(), dto);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMedicines([FromQuery] string term)
    {
        var result = await service.SearchMedicinesAsync(term);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}