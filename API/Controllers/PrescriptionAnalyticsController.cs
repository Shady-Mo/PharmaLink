using Application.DTOs.AI;
using Application.Services.AI;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/prescription-analytics")]
public sealed class PrescriptionAnalyticsController(IPrescriptionAnalyticsRagService service) : ControllerBase
{
    [HttpPost("ask")]
    [ProducesResponseType(typeof(PrescriptionAnalyticsAnswerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = $"{AppRoles.Admin} , {AppRoles.PharmacyAdmin} , {AppRoles.PharmacyAdmin}")]

    public async Task<IActionResult> Ask(
        [FromBody] PrescriptionAnalyticsQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "مطلوب إدخال السؤال التحليلي." });

        var result = await service.AskAsync(request.Question, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reindex")]
    [Authorize(Roles = $"{AppRoles.Admin} , {AppRoles.PharmacyAdmin} , {AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        var queuedCount = await service.QueueReindexAsync(cancellationToken);
        return Accepted(new { queuedCount });
    }
}
