namespace Application.Services
{
    public interface IPharmacyOrderService
    {
        Task<Result<PaginatedList<PharmacyOrderSummaryDTO>>> GetOrdersAsync(
            Guid pharmacyId,
            OrderQueryParametersDto query,
            CancellationToken cancellationToken = default);

        Task<Result<PharmacyOrderDetailDTO>> GetOrderByIdAsync(
            Guid pharmacyId,
            Guid orderId,
            CancellationToken cancellationToken = default);
    }
}
