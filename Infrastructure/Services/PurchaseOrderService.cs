using Application.DTOs.PurchaseOrder;
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

            if (po == null || po.Status != POStatus.Pending)
            {
                return false;
            }

            po.Status = POStatus.Approved;
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
            var result = await _context.PurchaseOrders.Where(p => p.BranchId == branchId && p.Status == POStatus.Pending).Select(p => new GetPurchaseOrderDTO
            {
                Id = p.Id,
                AiRationale = p.AiRationale,
                CreatedAt = p.CreatedAt,
                DrugName = p.Drug.GenericName,
                BranchName = p.Branch.BranchName,
                OrderedQuantity = p.OrderedQuantity,
                Status = p.Status
            }).ToListAsync();

            return result;
        }
    }
}
