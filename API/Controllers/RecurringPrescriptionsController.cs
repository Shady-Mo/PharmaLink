using Application.DTOs.RecurringPrescription;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Extensions;
using Domain.Constants;

namespace API.Controllers;

[Route("api/recurring-prescriptions")]
[ApiController]
[Authorize(Roles = AppRoles.Patient)]
public class RecurringPrescriptionsController(IRecurringPrescriptionService service) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringRequest request)
    {
        var result = await service.CreateAsync(User.GetUserId(), request);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetPatientRecurringAsync(User.GetUserId());
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRecurringRequest request)
    {
        var result = await service.UpdateAsync(id, User.GetUserId(), request);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id, User.GetUserId());
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPatch("{id}/pause")]
    public async Task<IActionResult> Pause(Guid id)
    {
        var result = await service.PauseAsync(id, User.GetUserId());
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPatch("{id}/resume")]
    public async Task<IActionResult> Resume(Guid id)
    {
        var result = await service.ResumeAsync(id, User.GetUserId());
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPost("runs/{runId}/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm(Guid runId, [FromQuery] string token)
    {
        var result = await service.ConfirmRunAsync(runId, token);
        return result.IsSuccess ? Redirect("/patient/prescriptions/recurring") : result.ToProblem();
    }

    [HttpPost("runs/{runId}/skip")]
    public async Task<IActionResult> Skip(Guid runId)
    {
        var result = await service.SkipRunAsync(runId, User.GetUserId());
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
