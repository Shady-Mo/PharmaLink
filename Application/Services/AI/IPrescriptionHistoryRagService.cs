using Application.DTOs.AI;

namespace Application.Services.AI;

public interface IPrescriptionHistoryRagService
{
    Task<PrescriptionHistoryAnswerResponse> AskAsync(
        Guid patientId,
        string question,
        CancellationToken cancellationToken = default);

    Task<int> QueueReindexAsync(Guid? patientId = null, CancellationToken cancellationToken = default);
}
