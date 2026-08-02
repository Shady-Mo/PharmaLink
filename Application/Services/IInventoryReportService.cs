using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IInventoryReportService
    {
        Task<IEnumerable<ForecastLogDto>> GetBranchForecastReportAsync(Guid branchId);
    }
}
