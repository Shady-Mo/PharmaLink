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

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
