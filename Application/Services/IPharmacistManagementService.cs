namespace Application.Services.Pharmacist;

public interface IPharmacistManagementService
{
    Task<Result<PharmacistResponseDTO>> CreatePharmacistAsync(
        Guid adminId,
        CreatePharmacistRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedList<PharmacistSummaryDTO>>> GetAllPharmacistsAsync(
        Guid adminId,
        PaginatedRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PharmacistResponseDTO>> GetPharmacistByIdAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default);

    Task<Result<PharmacistResponseDTO>> UpdatePharmacistAsync(
        Guid adminId,
        Guid pharmacistId,
        UpdatePharmacistRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AssignmentHistoryItemDTO>>> GetPharmacistHistoryAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default);

    Task<Result> DeletePharmacistAsync(
        Guid adminId,
        Guid pharmacistId,
        CancellationToken cancellationToken = default);
}
