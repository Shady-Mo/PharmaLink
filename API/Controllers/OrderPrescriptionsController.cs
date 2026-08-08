using Application.DTOs.Prescriptions;

namespace API.Controllers;

public class OrderPrescriptionsController(
    IOrderPrescriptionService orderPrescriptionService) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Patient)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPrescription([FromForm] UploadPrescriptionRequest request,
        CancellationToken ct)
    {
        var result = await orderPrescriptionService.UploadPrescriptionAsync(User.GetUserId(), request,
            $"{Request.Scheme}://{Request.Host}", ct);

        return result.IsSuccess
            ? Created($"/api/order-prescriptions/{result.Value!.Id}", result.Value)
            : result.ToProblem();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Pharmacist},{AppRoles.Admin},{AppRoles.PrescriptionReviewTeam}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrescriptionDetails(Guid id, CancellationToken ct)
    {
        var result =
            await orderPrescriptionService.GetPrescriptionDetailsAsync(id, User.GetUserId(), User.GetRoleName()!, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}/file")]
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Pharmacist},{AppRoles.Admin},{AppRoles.PrescriptionReviewTeam}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrescriptionFile(Guid id, CancellationToken ct)
    {
        var result =
            await orderPrescriptionService.GetPrescriptionFileAsync(id, User.GetUserId(), User.GetRoleName()!, ct);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName);
    }
}