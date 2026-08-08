using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IPharmacistDashboardService
    {
        Task<Result<PharmacistDailyMetricsDto>> GetDailyMetricsAsync(Guid branchId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<InventoryAlertDto>>> GetInventoryAlertsAsync(Guid branchId, int stockThreshold = 10, int expiryDaysThreshold = 90, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<FulfillmentTaskDto>>> GetPendingFulfillmentTasksAsync(Guid branchId, int limit = 5, CancellationToken cancellationToken = default);
    }
}
