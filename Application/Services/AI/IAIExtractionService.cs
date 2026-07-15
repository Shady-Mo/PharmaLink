namespace Application.Services.AI;

public interface IAIExtractionService
{
    Task<AIExtractionResult> ExtractMedicinesFromImageAsync(
        string absoluteImagePath,
        CancellationToken cancellationToken = default);
}