using Application.DTOs.PharmacyInventory.Request;
using Application.DTOs.PharmacyInventory.Response;

namespace Application.Services;

public interface IInventoryService
{
    Task<Result> ReserveStockAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default);

    Task<Result> ReserveStockBatchAsync(
        IEnumerable<(Guid BranchId, Guid DrugId, int Quantity)> reservations,
        CancellationToken cancellationToken = default);

    Result ReserveStockBatch(
        IEnumerable<PharmacyInventory> inventories,
        IEnumerable<(Guid BranchId, Guid DrugId, int Quantity)> reservations);

    Task<Result> ReleaseReservationAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default);

    Task<Result> ReleaseReservationBatchAsync(
        IEnumerable<(Guid BranchId, Guid DrugId, int Quantity)> releases,
        CancellationToken cancellationToken = default);

    Task<Result<GetPharmacyInventoryDTO>> CreateAsync(
        AddPharmacyInventoryDto dto, 
        CancellationToken cancellationToken = default);

    Task<Result<GetPharmacyInventoryDTO>> UpdateAsync(Guid inventoryId, 
        UpdatePharmacyInventoryDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid inventoryId, CancellationToken cancellationToken = default);

    Task<Result<PharmacyInventoryDto>> GetByIdAsync(Guid inventoryId,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedList<GetPharmacyInventoryDTO>>> GetInventoryAsync(
        GetPharmacyInventoryParamRequest parameters, 
        CancellationToken cancellationToken = default);

    Task<Result> AdjustStock(Guid id, AdjustStockDTO adjustStockDTO, CancellationToken cancellationToken = default);
}


