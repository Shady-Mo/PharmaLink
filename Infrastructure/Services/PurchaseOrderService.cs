using Application.DTOs.PurchaseOrder;
using Application.DTOs.Supplier;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class PurchaseOrderService(AppDbContext _context) : IPurchaseOrderService
    {

        public async Task<bool> ApprovePurchaseOrderAsync(Guid orderId, string userId)
        {
            var po = await _context.PurchaseOrders.FindAsync(orderId);

            if (po == null || po.Status != POStatus.PendingPharmacyApproval)
            {
                return false;
            }

            po.Status = POStatus.SentToSupplier;
            po.ApprovedAt = DateTime.UtcNow;

            po.ApprovedBy = userId;

            var inventoryItem = await _context.PharmacyInventories.Where(i => i.DrugId == po.DrugId && i.BranchId == po.BranchId).FirstOrDefaultAsync();

            inventoryItem.StockQuantity += po.OrderedQuantity;
            inventoryItem.ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(3));

            _context.Update(inventoryItem);
           

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<GetPurchaseOrderDTO>> GetPendingPurchaseOrders(Guid branchId)
        {
            var result = await _context.PurchaseOrders.Where(p => p.BranchId == branchId && p.Status == POStatus.PendingPharmacyApproval).Select(p => new GetPurchaseOrderDTO
            {
                Id = p.Id,
                DrugId = p.DrugId,
                AiRationale = p.AiRationale,
                CreatedAt = p.CreatedAt,
                DrugName = p.Drug.BrandName,
                BranchName = p.Branch.BranchName,
                OrderedQuantity = p.OrderedQuantity,
                Status = p.Status
            }).ToListAsync();

            return result;
        }

        public async Task<Result<PaginatedList<GetPurchaseOrderDTO>>> GetBranchOrdersAsync(Guid branchId, OrderFilterParams filterParams)
        {
            var query = _context.PurchaseOrders
                .Include(po => po.Drug)
                .Where(po => po.BranchId == branchId)
                .AsQueryable();

            if (filterParams.Status.HasValue)
            {
                query = query.Where(po => po.Status == filterParams.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filterParams.SearchTerm))
            {
                var searchTerm = filterParams.SearchTerm.Trim().ToLower();
                query = query.Where(po => po.Drug != null && po.Drug.BrandName.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(po => po.CreatedAt)
                .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                .Take(filterParams.PageSize)
                .Select(po => new GetPurchaseOrderDTO
                {
                    Id = po.Id,
                    DrugId = po.DrugId,
                    DrugName = po.Drug != null ? po.Drug.BrandName : "",
                    BranchName = po.Branch != null ? po.Branch.BranchName : "",
                    OrderedQuantity = po.OrderedQuantity,
                    Status = po.Status,
                    AiRationale = po.AiRationale,
                    CreatedAt = po.CreatedAt
                })
                .ToListAsync();

            var result = new PaginatedList<GetPurchaseOrderDTO>(orders, filterParams.PageNumber, totalCount, filterParams.PageSize);

            return Result.Success(result);
        }
    }
}
