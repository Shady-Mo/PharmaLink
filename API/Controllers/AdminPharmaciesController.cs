using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;
using Application.Services.Pharmacy;
using Domain.Constants;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/v1/admin-pharmacies")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminPharmaciesController(IAdminPharmacyService adminPharmacyService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllPharmacies(
            [FromQuery] GetAdminPharmaciesRequest request,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.GetAllPharmaciesAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPharmacy(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.GetPharmacyByIdAsync(id, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost]
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePharmacy(
            Guid id,
            [FromForm] AdminUpdatePharmacyDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.UpdatePharmacyAsync(id, dto, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeletePharmacy(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.SoftDeletePharmacyAsync(id, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangePharmacyStatus(
            Guid id,
            [FromBody] VerificationStatus status,
            CancellationToken cancellationToken)
        {
            var result = await adminPharmacyService.ChangePharmacyStatusAsync(id, status, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPost("{id}/assign-owner/{userId}")]
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
