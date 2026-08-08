using System.Security.Claims;
using Application.DTOs.OrderFulfillmentLeg.Requests;
using Application.DTOs.OrderFulfillmentLeg.Responses;

namespace Application.Services;

public interface IOrderFulfillmentLegService
{
    Task<Result<OrderFulfillmentLegDto>> GetByIdAsync(
        Guid legId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<OrderFulfillmentLegDto>> UpdateStatusAsync(
        Guid legId,
        UpdateLegStatusRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedList<BranchOrderRowDto>>> GetBranchOrdersAsync(
        GetBranchOrdersRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default
    );

    Task<Result<PharmacistOrderDetailsDto>> GetPharmacistOrderDetailsAsync(
        Guid orderId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
