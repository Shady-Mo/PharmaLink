namespace Application.Services.AI;

/// <summary>
/// The severity level of a detected drug-drug interaction.
/// </summary>
public enum InteractionSeverity
{
    None,
    Minor,
    Moderate,
    Severe,
    Contraindicated
}

/// <summary>
/// Describes a single interaction between two drugs.
/// </summary>
public sealed record DrugInteraction(
    string Drug1,
    string Drug2,
    InteractionSeverity Severity,
    string Description,
    string Recommendation
);

/// <summary>
/// The result of a drug interaction check for a list of drugs.
/// </summary>
public sealed record InteractionCheckResult(
    IReadOnlyList<string> CheckedDrugs,
    IReadOnlyList<DrugInteraction> Interactions,
    bool HasSevereInteractions,
    string Summary
);
