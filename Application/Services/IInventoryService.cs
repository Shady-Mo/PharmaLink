using Application.DTOs.PharmacyInventory.Request;
using Application.DTOs.PharmacyInventory.Response;

namespace Application.Services;

public interface IInventoryService
{
    Task<Result> ReserveStockAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default);

    Task<Result> ReleaseReservationAsync(Guid branchId, Guid drugId, int quantity,
        CancellationToken cancellationToken = default);

    Task<Result<PharmacyInventoryDto>> CreateAsync(AddPharmacyInventoryDto dto, CancellationToken cancellationToken = default);
    Task<Result<PharmacyInventoryDto>> UpdateAsync(UpdatePharmacyInventoryDto dto, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<PharmacyInventoryDto>>> GetInventoryAsync(GetPharmacyInventoryParamRequest parameters, CancellationToken cancellationToken = default);

}