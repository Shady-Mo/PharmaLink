using Application.DTOs.AI;
using Application.Services.AI;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/prescription-history")]
public sealed class PrescriptionHistoryController(IPrescriptionHistoryRagService service) : ControllerBase
{
    [HttpPost("ask")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(PrescriptionHistoryAnswerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ask(
        [FromBody] PrescriptionHistoryQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        var result = await service.AskAsync(User.GetUserId(), request.Question, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reindex")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Reindex(
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        var queuedCount = await service.QueueReindexAsync(patientId, cancellationToken);
        return Accepted(new { queuedCount });
    }

    [HttpPost("reindex/me")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReindexMine(CancellationToken cancellationToken)
    {
        var queuedCount = await service.QueueReindexAsync(User.GetUserId(), cancellationToken);
        return Accepted(new { queuedCount });
    }
}
