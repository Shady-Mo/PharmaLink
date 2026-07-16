using Application.DTOs.Patient;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize] // يضمن حظر أي مستخدم غير مسجل وعودة رد 401 تلقائيًا من الخادم
public class PatientsController(
    IPatientService patientService,
    ILogger<PatientsController> logger) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        // استخراج معرّف المستخدم بدعم كامل لجميع صيغ الـ Claims المستخدمة في مشروعك
        var userIdStr = User.FindFirst("UserID")?.Value          // مطابقة للتوكن الفعلي الخاص بك
                        ?? User.FindFirst("userId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var patientId))
        {
            logger.LogWarning("Unauthorized profile access attempt: Patient Claim 'UserID' not found or invalid.");
            return Unauthorized();
        }

        logger.LogInformation("Authenticated patient {PatientId} requested their profile details.", patientId);

        var result = await patientService.GetProfileAsync(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }


    // -------**-**-*-*-****-*-*-** //

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default)
    {
        // 1. التحقق التلقائي من صحة الحقول (Validation Check)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 2. استخراج معرّف المريض بأمان من الـ Token لضمان عدم تعديل مريض لملف مريض آخر
        var userIdStr = User.FindFirst("UserID")?.Value
                        ?? User.FindFirst("userId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var patientId))
        {
            logger.LogWarning("Unauthorized profile update attempt: Invalid token claims.");
            return Unauthorized();
        }

        logger.LogInformation("Authenticated patient {PatientId} requested a profile update.", patientId);

        // 3. استدعاء الخدمة لتحديث البيانات المسموحة
        var result = await patientService.UpdateProfileAsync(patientId, updateDto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }



}