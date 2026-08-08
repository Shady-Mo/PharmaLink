using Application.DTOs.Supplier;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface ISupplierOrderService
    {
        Task<Result<List<SupplierOrderDto>>> GetOrdersBySupplierAsync(Guid supplierId, POStatus? status = null);

        Task<Result> AcceptOrderAsync(Guid orderId, Guid supplierId);

        Task<Result> RejectOrderAsync(Guid orderId, Guid supplierId);

        Task<Result>  UpdateOrderStatusAsync(Guid orderId, Guid supplierId, POStatus newStatus);

        Task<Result<List<AvailableSupplierDto>>> GetSuppliersForDrugAsync(Guid drugId);
        Task<Result> AssignSupplierToOrderAsync(Guid orderId, Guid supplierId, Guid pharmacyBranchId);

        Task<Result> ReceiveOrderAsync(Guid orderId, Guid branchId);
    }
}
