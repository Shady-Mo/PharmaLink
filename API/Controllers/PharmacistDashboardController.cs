using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/v1/pharmacist/[controller]")]
    [Authorize(Roles = AppRoles.Pharmacist)]
    [ApiController]
    public class PharmacistDashboardController : ControllerBase
    {
        private readonly IPharmacistDashboardService _dashboardService;

        public PharmacistDashboardController(IPharmacistDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private Guid GetBranchId()
        {
            var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type == "BranchId")?.Value;
            return Guid.TryParse(branchIdClaim, out var branchId) ? branchId : Guid.Empty;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetDailyMetrics(CancellationToken cancellationToken)
        {
            var branchId = GetBranchId();
            var result = await _dashboardService.GetDailyMetricsAsync(branchId, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
        }

        [HttpGet("inventory-alerts")]
        public async Task<IActionResult> GetInventoryAlerts([FromQuery] int stockThreshold = 10, [FromQuery] int expiryThreshold = 90, CancellationToken cancellationToken = default)
        {
            var branchId = GetBranchId();
            var result = await _dashboardService.GetInventoryAlertsAsync(branchId, stockThreshold, expiryThreshold, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
        }

        [HttpGet("pending-tasks")]
        public async Task<IActionResult> GetPendingTasks([FromQuery] int limit = 5, CancellationToken cancellationToken = default)
        {
            var branchId = GetBranchId();
            var result = await _dashboardService.GetPendingFulfillmentTasksAsync(branchId, limit, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
        }
    }
}
