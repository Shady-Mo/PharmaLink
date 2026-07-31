using Application.DTOs.MedicalInquiry.Requests;
using Application.DTOs.MedicalInquiry.Responses;
using Application.Services.MedicalInquiry;

namespace API.Controllers;

public class MedicalInquiriesController(
    IMedicalInquiryService service) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(MedicalInquiryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMedicalInquiryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(User.GetUserId(), request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMine), new { }, result.Value)
            : result.ToProblem();
    }

    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalInquiryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await service.GetMineAsync(User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("review-team")]
    [Authorize(Roles = $"{AppRoles.PrescriptionReviewTeam},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalInquiryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForReviewTeam(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await service.GetForReviewTeamAsync(status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("review-team/metrics")]
    [Authorize(Roles = $"{AppRoles.PrescriptionReviewTeam},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(MedicalInquiryMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var result = await service.GetMetricsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id:guid}/answer")]
    [Authorize(Roles = $"{AppRoles.PrescriptionReviewTeam},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(MedicalInquiryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Answer(
        Guid id,
        [FromBody] AnswerMedicalInquiryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AnswerAsync(id, User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id:guid}/close")]
    [Authorize(Roles = $"{AppRoles.PrescriptionReviewTeam},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(MedicalInquiryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.CloseAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
