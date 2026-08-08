using Application.DTOs.Supplier;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Authorize(Roles = AppRoles.Supplier)]
    public class SupplierProfileController(ISupplierProfileService _supplierProfileService) : BaseApiController
    {

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _supplierProfileService.GetProfileAsync(User.GetUserId());
            return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateSupplierProfileDto dto)
        {
            var result = await _supplierProfileService.UpdateProfileAsync(User.GetUserId(), dto);
            return result.IsFailure ? BadRequest(result.Error) : Ok(new { Message = "تم تحديث البيانات بنجاح." });
        }
    }
}