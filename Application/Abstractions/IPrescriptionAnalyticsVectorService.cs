using Application.Services.AI.Models;

namespace Application.Abstractions
{
    public interface IPrescriptionAnalyticsVectorService
    {
        Task UpsertPrescriptionAsync(
            PrescriptionAnalyticsEmbeddingRecord record,
            CancellationToken cancellationToken = default);

        Task<List<PrescriptionAnalyticsSearchResult>> SearchAsync(
            string query,
            int topK = 30,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid prescriptionId,
            CancellationToken cancellationToken = default);
    }
}
