using API.Notification;
using Application.DTOs.Notification;

namespace Infrastructure.Services
{
    public class InventoryForecastingService(AppDbContext _context, UserManager<AppUser> userManager, IInventoryForecastingCalculator _calculator, INotificationService _notificationService) : IInventoryForecastingService
    {

        public async Task<Result> RunForecastingCycleAsync(Guid? branchId = null, int analysisDays = 30)
        {
            var query = _context.PharmacyInventories.AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(i => i.BranchId == branchId.Value);
            }

            var inventoryItems = await query
                .Include(i => i.Drug)
                .Include(i => i.Branch)
                .ToListAsync();

            DateTime startDate = DateTime.UtcNow.AddDays(-analysisDays);

            var notificationsToSend = new List<PoNotificationDto>();

            foreach (var item in inventoryItems)
            {
              
                int leadTimeDays = 3;
                int safetyStock = 10;
                double orderCost = 100.0;
                double holdingCost = item.Drug.Price > 0 ? (double)(item.Drug.Price * 0.2m) : 2.5;

                int totalSales = await _context.OrderItems
                     .Where(oi => oi.DrugId == item.DrugId
                               && oi.BranchId == item.BranchId
                               && oi.Order.CreatedAt >= startDate
                               && oi.Order.OrderStatus == OrderStatus.Completed
                           )
                     .SumAsync(oi => oi.QuantityNeeded);

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

                    bool pendingExists = await _context.PurchaseOrders
                        .AnyAsync(po => po.DrugId == item.DrugId
                                     && po.BranchId == item.BranchId
                                     && po.Status == POStatus.Pending);

                    if (!pendingExists)
                    {
                        var newPo = new PurchaseOrder
                        {
                            DrugId = item.DrugId,
                            BranchId = item.BranchId,
                            OrderedQuantity = eoq > 0 ? eoq : 30,
                            Status = POStatus.Pending,
                            AiRationale = rationale + $"EOQ recommends ordering {eoq} units.",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.PurchaseOrders.Add(newPo);
                        actionTaken = "PO_Created";

                        notificationsToSend.Add(new PoNotificationDto
                        {
                            BranchId = item.BranchId,
                            DrugName = item.Drug.GenericName,
                            CurrentStock = item.StockQuantity,
                            PredictedStockoutDate = depletionDate,
                            RecommendedOrderQuantity = eoq > 0 ? eoq : 100,
                            AiRationale = rationale + $"EOQ recommends ordering {eoq} units."
                        });
                    }
                    else
                    {
                        rationale += "A pending PO already exists. No new PO created.";
                        actionTaken = "PO_Pending_Exists";
                    }
                }

                item.ReorderPoint = rop;
                item.LastForecastDate = DateTime.UtcNow;

                var forecastLog = new InventoryForecastLog
                {
                    DrugId = item.DrugId,
                    BranchId = item.BranchId,
                    DrugName = item.Drug.GenericName,
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
                var user = _context.PharmacyAdmins.Where(p => p.Pharmacy.Branches.Any(b => b.BranchId == branchId)).FirstOrDefault();
                if(user != null)
                    email = user.Email!;
            }

            foreach (var notification in notificationsToSend)
            {
                await _notificationService.SendPoCreatedNotificationAsync(notification, email);
            }

            return Result.Success();
        }
    }
}