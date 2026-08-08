using Application.DTOs.AI.RAG;

namespace Application.Services.AI.RAG;

public interface IPrescriptionAnalyticsRagService
{
    Task<PrescriptionAnalyticsRagResponseDTO> QueryAnalyticsAsync(
        PrescriptionAnalyticsRagRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<int> ReindexPrescriptionsAsync(CancellationToken cancellationToken = default);

    Task IndexSinglePrescriptionAsync(Guid prescriptionReviewId, CancellationToken cancellationToken = default);
}
