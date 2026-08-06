using Application.DTOs.Supplier;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Authorize(Roles = AppRoles.Supplier)]
    public class SupplierDrugsController(ISupplierDrugService _supplierDrugService) : BaseApiController
    {

        [HttpGet]
        public async Task<IActionResult> GetMyDrugs([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _supplierDrugService.GetMyDrugsAsync(User.GetUserId(), search, pageNumber, pageSize);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new
            {
                Success = true,
                Data = result.Value.Drugs,
                Pagination = new
                {
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalCount = result.Value.TotalCount,
                    TotalPages = (int)Math.Ceiling(result.Value.TotalCount / (double)pageSize)
                }
            });
        }


        [HttpPost("{drugId:guid}")]
        public async Task<IActionResult> AddDrugToMyList(Guid drugId)
        {
            var result = await _supplierDrugService.AddDrugToMyListAsync(User.GetUserId(), drugId);
            return result.IsFailure ? BadRequest(result.Error) : Ok(new { Message = "تم إضافة الدواء لقائمتك بنجاح." });
        }

        [HttpDelete("{drugId:guid}")]
        public async Task<IActionResult> RemoveDrugFromMyList(Guid drugId)
        {
            var result = await _supplierDrugService.RemoveDrugFromMyListAsync(User.GetUserId(), drugId);
            return result.IsFailure ? BadRequest(result.Error) : Ok(new { Message = "تم إزالة الدواء من قائمتك." });
        }

        [HttpGet("search-global")]
        public async Task<IActionResult> SearchGlobalDrugs([FromQuery] string query)
        {
            // فحص سريع لو اليوزر مبعتش حاجة
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<AvailableDrugDto>());

            var result = await _supplierDrugService.SearchGlobalDrugsAsync(query);

            if (result.IsFailure)
                return BadRequest(new { Message = result.Error });

            // بنرجع الـ Data على طول عشان الـ AutoComplete في الفرونت إند بيستقبل Array مباشر
            return Ok(result.Value);
        }
    }
}