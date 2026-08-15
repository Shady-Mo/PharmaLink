using Application.DTOs.AI;

namespace Application.Services.AI;

public interface IPrescriptionAnalyticsRagService
{
    Task<PrescriptionAnalyticsAnswerResponse> AskAsync(
        string question,
        CancellationToken cancellationToken = default);

    Task<int> QueueReindexAsync(CancellationToken cancellationToken = default);
}
