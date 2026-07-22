namespace Infrastructure.Services
{
    public class PharmacistDashboardService : IPharmacistDashboardService
    {
        private readonly AppDbContext _context;

        public PharmacistDashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<PharmacistDailyMetricsDto>> GetDailyMetricsAsync(Guid branchId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;

            var pendingReviews = await _context.PrescriptionReviews
                .CountAsync(p => p.ReviewStatus == PrescriptionReviewStatus.PendingReview, cancellationToken);

            var completedReviewsToday = await _context.PrescriptionReviews
                .CountAsync(p => p.ReviewStatus != PrescriptionReviewStatus.PendingReview && p.ReviewedAt.HasValue && p.ReviewedAt.Value.Date == today, cancellationToken);

            var pendingOrders = await _context.OrderFulfillmentLegs
                .CountAsync(l => l.BranchId == branchId && (l.LegStatus == LegStatus.Assigned || l.LegStatus == LegStatus.Preparing), cancellationToken);

            var completedOrdersToday = await _context.OrderFulfillmentLegs
                .CountAsync(l => l.BranchId == branchId && (l.LegStatus == LegStatus.ReadyForPickup || l.LegStatus == LegStatus.Delivered) && l.CompletedAt.HasValue && l.CompletedAt.Value.Date == today, cancellationToken);

            var metrics = new PharmacistDailyMetricsDto
            {
                PendingPrescriptionReviews = pendingReviews,
                CompletedReviewsToday = completedReviewsToday,
                PendingFulfillmentOrders = pendingOrders,
                CompletedOrdersToday = completedOrdersToday
            };

            return Result.Success(metrics);
        }

        public async Task<Result<IEnumerable<InventoryAlertDto>>> GetInventoryAlertsAsync(Guid branchId, int stockThreshold = 10, int expiryDaysThreshold = 90, CancellationToken cancellationToken = default)
        {
            var targetExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(expiryDaysThreshold));

            var alerts = await _context.PharmacyInventories
                .Include(i => i.Drug)
                .Where(i => i.BranchId == branchId && (i.StockQuantity <= stockThreshold || i.ExpiryDate <= targetExpiryDate))
                .Select(i => new InventoryAlertDto
                {
                    DrugId = i.DrugId.ToString(),
                    BrandName = i.Drug.BrandName,
                    StockQuantity = i.StockQuantity,
                    ExpiryDate = i.ExpiryDate,
                    AlertType = i.StockQuantity <= stockThreshold ? "Low Stock" : "Expiring Soon"
                })
                .OrderBy(i => i.StockQuantity)
                .Take(10)
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<InventoryAlertDto>>(alerts);
        }

        public async Task<Result<IEnumerable<FulfillmentTaskDto>>> GetPendingFulfillmentTasksAsync(Guid branchId, int limit = 5, CancellationToken cancellationToken = default)
        {
            var tasks = await _context.OrderFulfillmentLegs
                .Include(l => l.Order)
                .ThenInclude(o => o.Items)
                .Where(l => l.BranchId == branchId && (l.LegStatus == LegStatus.Assigned || l.LegStatus == LegStatus.Preparing))
                .OrderBy(l => l.ReadyByEstimate)
                .Take(limit)
                .Select(l => new FulfillmentTaskDto
                {
                    LegId = l.LegId.ToString(),
                    OrderId = l.OrderId.ToString(),
                    ReadyByEstimate = l.ReadyByEstimate,
                    TotalAmount = l.Order.TotalAmount,
                    ItemsCount = l.Order.Items.Count(i => i.BranchId == branchId)
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<FulfillmentTaskDto>>(tasks);
        }
    }
}
