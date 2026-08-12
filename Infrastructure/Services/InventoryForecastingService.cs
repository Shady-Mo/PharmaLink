using API.Notification;
using Application.DTOs.Notification;

namespace Infrastructure.Services
{
    public class InventoryForecastingService(
        AppDbContext _context,
        UserManager<AppUser> userManager,
        IInventoryForecastingCalculator _calculator,
        INotificationService _notificationService) : IInventoryForecastingService
    {
        public async Task<Result> RunForecastingCycleAsync(Guid? branchId = null, int analysisDays = 30)
        {
            var query = _context.PharmacyInventories.AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(i => i.BranchId == branchId.Value);
            }

            var inventoryItems = await query
                .AsNoTracking()
                .Include(i => i.Drug)
                .Include(i => i.Branch)
                .ToListAsync();

            if (!inventoryItems.Any())
                return Result.Success();

            DateTime startDate = DateTime.UtcNow.AddDays(-analysisDays);


            var branchIds = inventoryItems.Select(i => i.BranchId).Distinct().ToList();
            var drugIds = inventoryItems.Select(i => i.DrugId).Distinct().ToList();

            var salesDataList = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.BranchId != null && branchIds.Contains(oi.BranchId.Value)
                          && drugIds.Contains(oi.DrugId)
                          && oi.Order.CreatedAt >= startDate
                          && oi.Order.OrderStatus == OrderStatus.Completed)
                .GroupBy(oi => new { oi.BranchId, oi.DrugId })
                .Select(g => new
                {
                    g.Key.BranchId,
                    g.Key.DrugId,
                    TotalQuantity = g.Sum(oi => oi.QuantityNeeded)
                })
                .ToListAsync();

            var salesDictionary = salesDataList
                .ToDictionary(x => (x.BranchId, x.DrugId), x => x.TotalQuantity);


            var pendingPosList = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(po => branchIds.Contains(po.BranchId)
                          && drugIds.Contains(po.DrugId)
                          && po.Status == POStatus.PendingPharmacyApproval)
                .Select(po => new { po.BranchId, po.DrugId })
                .Distinct()
                .ToListAsync();

            var pendingPoSet = new HashSet<(Guid BranchId, Guid DrugId)>(
                pendingPosList.Select(p => (p.BranchId, p.DrugId)));


            var inventoriesToUpdateQuery = _context.PharmacyInventories.AsQueryable();

            if (branchId.HasValue)
            {
                inventoriesToUpdateQuery = inventoriesToUpdateQuery.Where(i => i.BranchId == branchId.Value);
            }
            var trackedInventories = await inventoriesToUpdateQuery
                .ToDictionaryAsync(i => i.InventoryId);


            var notificationsToSend = new List<PoNotificationDto>();

            var set = new HashSet<Guid>();

            foreach (var item in inventoryItems)
            {
                int leadTimeDays = 3;
                int safetyStock = 10;
                double orderCost = 100.0;
                double holdingCost = item.Drug.Price > 0 ? (double)(item.Drug.Price * 0.2m) : 2.5;

                salesDictionary.TryGetValue((item.BranchId, item.DrugId), out int totalSales);

                double add = _calculator.CalculateAverageDailyDemand(totalSales, analysisDays);
                int rop = _calculator.CalculateReorderPoint(add, leadTimeDays, safetyStock);
                DateTime? depletionDate = _calculator.CalculateStockDepletionDate(item.StockQuantity, add);
                int eoq = _calculator.CalculateEconomicOrderQuantity(add, orderCost, holdingCost);

                string rationale = $"Based on {analysisDays} days of data, ADD is {add:F2}. " +
                                   $"Lead time is {leadTimeDays} days with {safetyStock} safety stock. " +
                                   $"Reorder point calculated at {rop}. ";

                string actionTaken = "Stock_Sufficient";

                if (item.StockQuantity <= rop)
                {
                    rationale += $"Current stock ({item.StockQuantity}) is below or equal to ROP ({rop}). ";

                    bool pendingExists = pendingPoSet.Contains((item.BranchId, item.DrugId));

                    if (!pendingExists)
                    {
                        var newPo = new PurchaseOrder
                        {
                            DrugId = item.DrugId,
                            BranchId = item.BranchId,
                            OrderedQuantity = eoq > 0 ? eoq : 30,
                            Status = POStatus.PendingPharmacyApproval,
                            AiRationale = rationale + $"EOQ recommends ordering {eoq} units.",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.PurchaseOrders.Add(newPo);
                        actionTaken = "PO_Created";

                        if (!set.Contains(item.BranchId))
                        {
                            notificationsToSend.Add(new PoNotificationDto
                            {
                                BranchId = item.BranchId,
                                BranchName = item.Branch.BranchName
                            });
                            set.Add(item.BranchId);
                        }
                        
                        
                    }
                    else
                    {
                        rationale += "A pending PO already exists. No new PO created.";
                        actionTaken = "PO_Pending_Exists";
                    }
                }

                if (trackedInventories.TryGetValue(item.InventoryId, out var inventoryToUpdate))
                {
                    inventoryToUpdate.ReorderPoint = rop;
                    inventoryToUpdate.LastForecastDate = DateTime.UtcNow;
                }

                var forecastLog = new InventoryForecastLog
                {
                    DrugId = item.DrugId,
                    BranchId = item.BranchId,
                    DrugName = !string.IsNullOrWhiteSpace(item.Drug.BrandName) ? item.Drug.BrandName : item.Drug.ArabicName,
                    ForecastDate = DateTime.UtcNow,
                    AverageDailyDemand = add,
                    ReorderPoint = rop,
                    PredictedDemand = (int)(add * leadTimeDays),
                    PredictedStockoutDate = depletionDate,
                    ActionTaken = actionTaken,
                    ConfidenceScore = 0.95m,
                    AiRationale = rationale
                };

                _context.InventoryForecastLogs.Add(forecastLog);
            }

            await _context.SaveChangesAsync();

            string email = "ohany3051@gmail.com";

            if (branchId != null)
            {
                var user = await _context.PharmacyAdmins
                    .AsNoTracking()
                    .Where(p => p.Pharmacy.Branches.Any(b => b.BranchId == branchId))
                    .FirstOrDefaultAsync();

                if (user != null && !string.IsNullOrEmpty(user.Email))
                    email = user.Email;
            }

            foreach (var notification in notificationsToSend)
            {
                await _notificationService.SendPoCreatedNotificationAsync(notification, email);
            }

            return Result.Success();
        }
    }
}