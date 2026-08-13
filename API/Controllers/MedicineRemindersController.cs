using Application.DTOs.MedicineReminder;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Extensions;
using Domain.Constants;

namespace API.Controllers;

[Route("api/v1/medicine-reminders")]
[ApiController]
[Authorize(Roles = AppRoles.Patient)]
public class MedicineRemindersController(IMedicineReminderService service) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReminderRequest request)
    {
        var result = await service.CreateAsync(User.GetUserId(), request);
     
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetPatientRemindersAsync(User.GetUserId());
        
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateReminderRequest request)
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

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var result = await service.ToggleAsync(id, User.GetUserId());
        
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
