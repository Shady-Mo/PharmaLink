namespace Application.Services.AI;

/// <summary>
/// Contains structured information about a drug retrieved from the AI.
/// </summary>
public sealed record DrugInfoResult(
    string DrugName,
    string? ArabicName,
    string? GenericName,
    string? Category,
    string? Description,
    string? Indications,
    string? Contraindications,
    string? SideEffects,
    string? Dosage,
    string? StorageInstructions,
    bool RequiresPrescription,
    bool IsAvailableInSystem
);
