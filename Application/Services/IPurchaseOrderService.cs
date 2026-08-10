using Application.DTOs.PurchaseOrder;
using Application.DTOs.Supplier;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IPurchaseOrderService
    {
        Task<bool> ApprovePurchaseOrderAsync(Guid orderId, string userId);

        Task<List<GetPurchaseOrderDTO>> GetPendingPurchaseOrders(Guid branchId);
        Task<Result<PaginatedList<GetPurchaseOrderDTO>>> GetBranchOrdersAsync(Guid branchId, OrderFilterParams filterParams);
    }
}
