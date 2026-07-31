using Application.Services.AI.Models;

namespace Application.Services.AI;

public interface IPrescriptionExtractionService
{
    Task<AIExtractionResult> ExtractAsync(
        AIFileContent file,
        CancellationToken cancellationToken = default);
}
