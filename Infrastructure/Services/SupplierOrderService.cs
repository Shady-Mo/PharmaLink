using Application.DTOs.Supplier;
using Google;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class SupplierOrderService(AppDbContext _context) : ISupplierOrderService
    {

        public async Task<Result<List<SupplierOrderDto>>> GetOrdersBySupplierAsync(Guid supplierId, POStatus? status = null)
        {
            var query = _context.PurchaseOrders
                .Include(po => po.Branch)
                .Include(po => po.Drug)
                .Where(po => po.SupplierId == supplierId);

            if (status.HasValue)
            {
                query = query.Where(po => po.Status == status.Value);
            }

            var orders = await query.OrderByDescending(po => po.CreatedAt).ToListAsync();

            var orderDtos = orders.Select(po => new SupplierOrderDto
            {
                OrderId = po.Id,
                PharmacyBranchName = po.Branch?.BranchName,
                OrderedAt = po.CreatedAt,
                CurrentStatus = po.Status.ToString(),
                DrugId = po.DrugId,
                DrugName = po.Drug?.BrandName,
                RequestedQuantity = po.OrderedQuantity
            }).ToList();

            return Result.Success(orderDtos);
        }

        public async Task<Result> AcceptOrderAsync(Guid orderId, Guid supplierId)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == orderId && po.SupplierId == supplierId);

            if (order == null)
                return Result.Failure(SupplierOrderErrors.NotFound);

            if (order.Status != POStatus.SentToSupplier)
                return Result.Failure(SupplierOrderErrors.BadRequest);

            order.Status = POStatus.AcceptedBySupplier;

            await _context.SaveChangesAsync();
            return Result.Success(true);
        }

        public async Task<Result> RejectOrderAsync(Guid orderId, Guid supplierId)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == orderId && po.SupplierId == supplierId);

            if (order == null)
                return Result.Failure(SupplierOrderErrors.NotFound);

            if (order.Status != POStatus.SentToSupplier)
                return Result.Failure(SupplierOrderErrors.BadRequest);



            order.Status = POStatus.RejectedBySupplier;

            await _context.SaveChangesAsync();
            return Result.Success(true);
        }

        public async Task<Result> UpdateOrderStatusAsync(Guid orderId, Guid supplierId, POStatus newStatus)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == orderId && po.SupplierId == supplierId);

            if (order == null)
                return Result.Failure(SupplierOrderErrors.NotFound);


            var allowedStatuses = new[] { POStatus.ProcessingBySupplier, POStatus.Shipped };

            if (!allowedStatuses.Contains(newStatus))
                return Result.Failure(SupplierOrderErrors.BadRequest);

            order.Status = newStatus;

            await _context.SaveChangesAsync();
            return Result.Success(true);
        }

        public async Task<Result<List<AvailableSupplierDto>>> GetSuppliersForDrugAsync(Guid drugId)
        {
            var suppliers = await _context.SupplierDrugs
                .Where(sd => sd.DrugId == drugId)
                .Select(sd => new AvailableSupplierDto
                {
                    SupplierId = sd.SupplierId,
                    SupplierName = sd.Supplier.FullName
                })
                .ToListAsync();

            if (!suppliers.Any())
            {
                return Result.Failure<List<AvailableSupplierDto>>(SupplierOrderErrors.NoSuppliersFoundForDrug);
            }

            return Result.Success(suppliers);
        }

        public async Task<Result> AssignSupplierToOrderAsync(Guid orderId, Guid supplierId, Guid branchId)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == orderId && po.BranchId == branchId);

            if (order == null)
                return Result.Failure(SupplierOrderErrors.NotFound);

            if (order.Status != POStatus.PendingPharmacyApproval)
                return Result.Failure(SupplierOrderErrors.BadRequest);

            var supplierHasDrug = await _context.SupplierDrugs
                .AnyAsync(sd => sd.SupplierId == supplierId && sd.DrugId == order.DrugId);

            if (!supplierHasDrug)
                return Result.Failure(SupplierOrderErrors.SupplierDoesNotHaveDrug);

            order.SupplierId = supplierId;
            order.Status = POStatus.SentToSupplier;
            order.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ReceiveOrderAsync(Guid orderId, Guid branchId)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == orderId && po.BranchId == branchId);

            if (order == null)
                return Result.Failure(SupplierOrderErrors.NotFound);

            if (order.Status != POStatus.Shipped)
                return Result.Failure(SupplierOrderErrors.BadRequest);

            order.Status = Domain.Entities.POStatus.Delivered;

            var inventory = await _context.PharmacyInventories
                .FirstOrDefaultAsync(i => i.BranchId == branchId && i.DrugId == order.DrugId);

            if (inventory == null)
            {
                inventory = new PharmacyInventory
                {
                    InventoryId = Guid.NewGuid(),
                    BranchId = branchId,
                    DrugId = order.DrugId,
                    ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2)),
                    StockQuantity = 0
                };
                _context.PharmacyInventories.Add(inventory);
            }

            inventory.StockQuantity += order.OrderedQuantity;
            inventory.LastSyncedAt = DateTime.UtcNow; 

            await _context.SaveChangesAsync();

            return Result.Success();
        }
    }
}
