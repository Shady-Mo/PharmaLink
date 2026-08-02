using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class InventoryReportService(AppDbContext _context) : IInventoryReportService
    {
        
        public async Task<IEnumerable<ForecastLogDto>> GetBranchForecastReportAsync(Guid branchId)
        {
            var reports = await _context.InventoryForecastLogs
                .Where(log => log.BranchId == branchId)
                .OrderByDescending(log => log.ForecastDate)
                .Take(50)
                .Select(log => new ForecastLogDto
                {
                    DrugId = log.DrugId,
                    ForecastDate = log.ForecastDate,
                    PredictedStockoutDate = log.PredictedStockoutDate,
                    AiRationale = log.AiRationale
                })
                .ToListAsync();

            return reports;
        }
    }
}
