namespace Application.Services.AI;

/// <summary>
/// Provides AI-powered drug information lookup and interaction checking.
///
/// Design decision: This is a separate interface from IPharmacyAssistantService
/// following the Interface Segregation Principle. Clients that only need structured
/// drug data (e.g. a drug detail page) should not depend on the full chat interface.
///
/// Implementations are free to use SK prompt functions, native database plugins,
/// or a combination of both under the hood.
/// </summary>
public interface IDrugInfoService
{
    /// <summary>
    /// Retrieves structured information about a drug using AI augmented with
    /// the pharmacy's own drug database.
    /// </summary>
    /// <param name="drugName">The drug name (brand or generic).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured drug information, or null if the drug is unknown.</returns>
    Task<DrugInfoResult?> GetDrugInfoAsync(
        string drugName,
        CancellationToken ct = default);

    /// <summary>
    /// Checks for known interactions between a list of drugs.
    /// Uses an AI-powered analysis augmented by established interaction databases.
    /// </summary>
    /// <param name="drugNames">2 or more drug names to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A structured result listing all detected interactions.</returns>
    Task<InteractionCheckResult> CheckInteractionsAsync(
        IReadOnlyList<string> drugNames,
        CancellationToken ct = default);
}
