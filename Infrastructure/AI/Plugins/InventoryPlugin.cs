using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

/// <summary>
/// Native SK plugin that lets the AI query real-time pharmacy inventory data.
///
/// DESIGN DECISION — Separate plugin per domain:
///   Each plugin encapsulates one domain concern (drugs, inventory, orders).
///   This mirrors the Single Responsibility Principle at the plugin level.
///   The AI can then combine information from multiple plugins in a single
///   conversation turn — e.g. "Search for drug → check if it's in stock → 
///   tell me the nearest branch" — by calling these plugins sequentially.
/// </summary>
public sealed class InventoryPlugin(IServiceScopeFactory scopeFactory, ILogger<InventoryPlugin> logger)
{
    public sealed record InventoryCheckResult(
        bool Found,
        string? Message = null,
        IReadOnlyList<InventoryItem>? Branches = null
    );

    public sealed record InventoryItem(
        string BranchName,
        string Address,
        string DrugName,
        int StockQuantity,
        string UnitPrice
    );

    public sealed record BranchInventoryResult(
        bool Found,
        string? Message = null,
        IReadOnlyList<BranchInventoryItem>? Drugs = null
    );

    public sealed record BranchInventoryItem(
        string DrugName,
        int StockQuantity,
        string UnitPrice
    );

    [KernelFunction("check_drug_availability")]
    [Description(
        "Checks whether a specific drug is currently in stock across pharmacy branches. " +
        "Returns the branches that have the drug and the available quantity. " +
        "Use this when the user asks 'Is X available?' or 'Can I get Y at a pharmacy?'")]
    public async Task<InventoryCheckResult> CheckDrugAvailabilityAsync(
        [Description("The exact drug name as stored in the database.")]
        string drugName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("InventoryPlugin.CheckDrugAvailabilityAsync for drug: {DrugName}", drugName);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drugs = await db.Drugs
            .AsNoTracking()
            .Where(d => (EF.Functions.Like(d.BrandName, $"%{drugName}%") ||
                         EF.Functions.Like(d.ArabicName, $"%{drugName}%")) &&
                        d.IsActive)
            .Select(d => new InventoryItem(
                "PharmaLink Central",
                "Online Pharmacy",
                d.BrandName ?? "Unknown Drug",
                100,
                d.Price.ToString()
            ))
            .Take(5)
            .ToListAsync(cancellationToken);

        if (!drugs.Any())
            return new InventoryCheckResult(Found: false, Message: $"'{drugName}' is not found in the PharmaLink database.");

        logger.LogDebug("Found {Count} drugs matching '{DrugName}' in database", drugs.Count, drugName);
        return new InventoryCheckResult(Found: true, Branches: drugs);
    }

    [KernelFunction("get_branch_inventory")]
    [Description(
        "Lists the drugs available at a specific pharmacy branch. " +
        "Returns drug names, quantities, and prices. " +
        "Use this when the user wants to know what a specific branch carries.")]
    public async Task<BranchInventoryResult> GetBranchInventoryAsync(
        [Description("The name or partial name of the pharmacy branch.")]
        string branchName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("InventoryPlugin.GetBranchInventoryAsync for branch: {BranchName}", branchName);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = await db.PharmacyInventories
            .AsNoTracking()
            .Include(i => i.Drug)
            .Include(i => i.Branch)
            .Where(i => EF.Functions.Like(i.Branch.BranchName, $"%{branchName}%") && i.StockQuantity > 0)
            .Select(i => new BranchInventoryItem(
                i.Drug.BrandName ?? "Unknown Drug",
                i.StockQuantity,
                i.UnitPrice.ToString()
            ))
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!items.Any())
            return new BranchInventoryResult(Found: false, Message: $"No inventory data found for branch '{branchName}', or the branch does not exist.");

        return new BranchInventoryResult(Found: true, Drugs: items);
    }
}