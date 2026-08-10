using Application.DTOs.AI;
using Application.Services.AI.Models;

namespace Application.Services.AI;

public interface IMedicineImageExtractionService
{
    Task<MedicineImageExtractionResponseDTO> ExtractAsync(
        AIFileContent file,
        CancellationToken cancellationToken = default);
}
