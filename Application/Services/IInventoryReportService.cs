using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IInventoryReportService
    {
        Task<(List<ForecastLogDto> Items, int TotalCount)> GetBranchForecastReportAsync(Guid branchId, int pageNumber, int pageSize);
    }
}
