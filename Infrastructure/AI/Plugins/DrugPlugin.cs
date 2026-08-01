using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

/// <summary>
/// Native (code-based) Semantic Kernel plugin that gives the AI model
/// direct, read-only access to the pharmacy's drug catalogue.
///
/// DESIGN DECISION — Native plugins vs. prompt functions:
///   Native plugins execute real C# code with real database queries. This
///   is essential for data that must be accurate and up-to-date (drug names,
///   availability). Prompt functions (pure text templates) are used for
///   tasks that benefit from the model's language understanding (summaries,
///   interaction explanations). We use both: native plugins for data
///   retrieval, prompt functions for reasoning over that data.
///
/// DESIGN DECISION — Read-only database access:
///   All plugin functions only READ from the database. No plugin can create,
///   update, or delete records. This is a security boundary — even if the AI
///   is somehow manipulated via prompt injection, it cannot modify data.
///
/// DESIGN DECISION — IServiceScopeFactory (Singleton-safe Scoped access):
///   The Kernel (and its plugins) is registered as Singleton.
///   AppDbContext is registered as Scoped.
///   Injecting a Scoped service into a Singleton causes a "captive dependency"
///   bug where the same DbContext is reused across requests, leading to
///   connection leaks, stale data, and thread-safety violations.
///   Solution: inject IServiceScopeFactory and create a short-lived scope
///   inside each plugin method. This is the official Microsoft pattern for
///   this scenario.
/// </summary>
public sealed class DrugPlugin(IServiceScopeFactory scopeFactory, ILogger<DrugPlugin> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // -------------------------------------------------------------------------
    //  Plugin functions — each method is decorated with [KernelFunction] so SK
    //  can auto-discover and auto-invoke them during function calling rounds.
    // -------------------------------------------------------------------------

    [KernelFunction("get_drug_info")]
    [Description(
        "Retrieves detailed information about a specific drug from the pharmacy database, " +
        "including its generic name, category, dosage forms, and whether it requires a prescription. " +
        "Use this when the user asks about a specific medication by name.")]
    public async Task<string> GetDrugInfoAsync(
        [Description(
            "The drug name — brand name (e.g. 'Augmentin') or generic name (e.g. 'Amoxicillin'). Partial names are accepted.")]
        string drugName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DrugPlugin.GetDrugInfoAsync called for drug: {DrugName}", drugName);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drug = await db.Drugs
            .AsNoTracking()
            .Where(d => EF.Functions.Like(d.BrandName, $"%{drugName}%") ||
                        EF.Functions.Like(d.GenericName, $"%{drugName}%"))
            .Select(d => new
            {
                Id = d.DrugId,
                Name = d.BrandName,
                GenericName = d.GenericName,
                Category = d.DrugClass,
                d.Form,
                d.Strength,
                d.RequiresPrescription,
                d.Manufacturer
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (drug is null)
        {
            logger.LogWarning("[DEBUG-PLUGIN] Drug '{DrugName}' not found in database", drugName);
            var notFoundMsg = $"The drug '{drugName}' was not found in the PharmaLink database. Provide general information based on medical knowledge, but note it may not be available in the system.";
            logger.LogWarning("[DEBUG-PLUGIN] Returning not found message: {Msg}", notFoundMsg);
            return notFoundMsg;
        }

        logger.LogWarning("[DEBUG-PLUGIN] Drug '{DrugName}' found: {DrugId}", drugName, drug.Id);
        var jsonResult = JsonSerializer.Serialize(drug, JsonOptions);
        logger.LogWarning("[DEBUG-PLUGIN] Serialized JSON string to return to Gemini: {Json}", jsonResult);
        return jsonResult;
    }

    [KernelFunction("search_drugs")]
    [Description(
        "Searches for drugs whose names start with or contain the given prefix. " +
        "Returns up to 8 matching drugs. " +
        "Use this when the user is looking for a type of medication or asks 'do you have X?'")]
    public async Task<string> SearchDrugsAsync(
        [Description("Drug name prefix or partial name to search for (e.g. 'amox', 'para', 'ibu').")]
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DrugPlugin.SearchDrugsAsync called with term: {SearchTerm}", searchTerm);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drugs = await db.Drugs
            .AsNoTracking()
            .Where(d => EF.Functions.Like(d.BrandName, $"%{searchTerm}%") ||
                        EF.Functions.Like(d.GenericName, $"%{searchTerm}%") ||
                        EF.Functions.Like(d.ArabicName, $"%{searchTerm}%"))
            .Select(d => new
                { Id = d.DrugId, Name = d.BrandName, GenericName = d.GenericName, Category = d.DrugClass, d.Form })
            .Take(8)
            .ToListAsync(cancellationToken);

        if (!drugs.Any())
        {
            logger.LogWarning("[DEBUG-PLUGIN] Search term '{SearchTerm}' yielded no results", searchTerm);
            return $"No drugs matching '{searchTerm}' were found in the PharmaLink database.";
        }

        logger.LogWarning("[DEBUG-PLUGIN] Search returned {Count} drugs", drugs.Count);
        var jsonResult = JsonSerializer.Serialize(drugs, JsonOptions);
        logger.LogWarning("[DEBUG-PLUGIN] Serialized JSON string to return to Gemini: {Json}", jsonResult);
        return jsonResult;
    }

    [KernelFunction("get_drugs_by_category")]
    [Description(
        "Retrieves a list of drugs belonging to a specific therapeutic category. " +
        "Use this when the user asks about a class of drugs (e.g. 'antibiotics', 'painkillers', 'antidiabetics').")]
    public async Task<string> GetDrugsByCategoryAsync(
        [Description("Therapeutic category name (e.g. 'Antibiotics', 'NSAIDs', 'Antihypertensives').")]
        string category,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DrugPlugin.GetDrugsByCategoryAsync called for category: {Category}", category);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drugs = await db.Drugs
            .AsNoTracking()
            .Where(d => EF.Functions.Like(d.DrugClass, $"%{category}%"))
            .Select(d => new { Name = d.BrandName, GenericName = d.GenericName, d.Form, d.Strength })
            .Take(10)
            .ToListAsync(cancellationToken);

        if (!drugs.Any())
        {
            logger.LogWarning("[DEBUG-PLUGIN] Category '{Category}' yielded no results", category);
            return $"No drugs in category '{category}' were found in the PharmaLink database.";
        }

        logger.LogWarning("[DEBUG-PLUGIN] Category returned {Count} drugs", drugs.Count);
        var jsonResult = JsonSerializer.Serialize(drugs, JsonOptions);
        logger.LogWarning("[DEBUG-PLUGIN] Serialized JSON string to return to Gemini: {Json}", jsonResult);
        return jsonResult;
    }
}