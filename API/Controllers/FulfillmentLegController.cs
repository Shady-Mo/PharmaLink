using Application.DTOs.FulfillmentLeg.Requests;
using Application.Services.FulfillmentLeg;

namespace API.Controllers;

[Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.Admin}")]
public class FulfillmentLegController(ILegStatusTransitionService legService) : BaseApiController
{
    [HttpPatch("{legId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLegStatus(
        Guid legId,
        [FromBody] PatchLegStatusRequestDTO request,
        CancellationToken cancellationToken)
    {
        var role = User.GetRoleName();

        if (role == AppRoles.Admin)
        {
            var adminResult = await legService.UpdateLegStatusForAdminAsync(
                legId, request.Status, request.AuditReason, cancellationToken);

            return adminResult.IsSuccess ? NoContent() : adminResult.ToProblem();
        }
        else
        {
            var branchIds = User.GetBranchIds();
            var result = await legService.UpdateLegStatusAsync(
                legId, request.Status, branchIds, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}