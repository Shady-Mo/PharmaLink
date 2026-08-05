using Application.DTOs.Supplier;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = AppRoles.Supplier)]
    public class SupplierOrdersController(ISupplierOrderService _supplierOrderService) : BaseApiController
    {
        private Guid GetSupplierId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var id))
                throw new UnauthorizedAccessException("المستخدم غير مصرح له.");

            return id;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] POStatus? status)
        {
            var supplierId = GetSupplierId();

            var result = await _supplierOrderService.GetOrdersBySupplierAsync(supplierId, status);

            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        [HttpPost("{orderId:guid}/accept")]
        public async Task<IActionResult> AcceptOrder(Guid orderId)
        {
            var supplierId = GetSupplierId();

            var result = await _supplierOrderService.AcceptOrderAsync(orderId, supplierId);

            if (result.IsFailure)
            {
                if (result.Error == SupplierOrderErrors.NotFound)
                    return NotFound(result.Error);

                return BadRequest(result.Error);
            }

            return Ok(new { Message = "تم قبول الطلب بنجاح." });
        }

        [HttpPost("{orderId:guid}/reject")]
        public async Task<IActionResult> RejectOrder(Guid orderId)
        {
            var supplierId = GetSupplierId();

            var result = await _supplierOrderService.RejectOrderAsync(orderId, supplierId);

            if (result.IsFailure)
            {
                if (result.Error == SupplierOrderErrors.NotFound)
                    return NotFound(result.Error);

                return BadRequest(result.Error);
            }

            return Ok(new { Message = "تم رفض الطلب." });
        }

        [HttpPut("{orderId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            var supplierId = GetSupplierId();

            var result = await _supplierOrderService.UpdateOrderStatusAsync(orderId, supplierId, dto.NewStatus);

            if (result.IsFailure)
            {
                if (result.Error == SupplierOrderErrors.NotFound)
                    return NotFound(result.Error);

                return BadRequest(result.Error);
            }

            return Ok(new { Message = "تم تحديث حالة الطلب بنجاح." });
        }
    }
}
