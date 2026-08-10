using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class InventoryReportService(AppDbContext _context) : IInventoryReportService
    {
        
        public async Task<(List<ForecastLogDto> Items, int TotalCount)> GetBranchForecastReportAsync(Guid branchId, int pageNumber, int pageSize)
        {
            var query =  _context.InventoryForecastLogs
                .Where(log => log.BranchId == branchId);

            var totalCount = await query.CountAsync();

            var reports = await query
                .OrderByDescending(log => log.ForecastDate)
                .OrderByDescending(log => log.ForecastDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new ForecastLogDto
                {
                    DrugId = log.DrugId,
                    DrugName = log.DrugName,
                    ForecastDate = log.ForecastDate,
                    PredictedStockoutDate = log.PredictedStockoutDate,
                    AiRationale = log.AiRationale
                })
                .ToListAsync();

            return (reports, totalCount);
        }
    }
}
