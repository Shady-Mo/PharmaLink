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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [KernelFunction("check_drug_availability")]
    [Description(
        "Checks whether a specific drug is currently in stock across pharmacy branches. " +
        "Returns the branches that have the drug and the available quantity. " +
        "Use this when the user asks 'Is X available?' or 'Can I get Y at a pharmacy?'")]
    public async Task<string> CheckDrugAvailabilityAsync(
        [Description("The exact drug name as stored in the database.")]
        string drugName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("InventoryPlugin.CheckDrugAvailabilityAsync for drug: {DrugName}", drugName);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var inventoryItems = await db.PharmacyInventories
            .AsNoTracking()
            .Include(i => i.Drug)
            .Include(i => i.Branch)
            .Where(i => (EF.Functions.Like(i.Drug.BrandName, $"%{drugName}%") ||
                         EF.Functions.Like(i.Drug.GenericName, $"%{drugName}%")) &&
                        i.StockQuantity > 0)
            .Select(i => new
            {
                BranchName = i.Branch.BranchName,
                Address = i.Branch.AddressLine + ", " + i.Branch.City,
                DrugName = i.Drug.BrandName,
                i.StockQuantity,
                i.UnitPrice
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        if (!inventoryItems.Any())
            return
                $"'{drugName}' is currently out of stock at all PharmaLink branches. Consider checking back later or ask your doctor about an alternative.";

        logger.LogDebug("Found {Count} branches with '{DrugName}' in stock", inventoryItems.Count, drugName);
        return JsonSerializer.Serialize(inventoryItems, JsonOptions);
    }

    [KernelFunction("get_branch_inventory")]
    [Description(
        "Lists the drugs available at a specific pharmacy branch. " +
        "Returns drug names, quantities, and prices. " +
        "Use this when the user wants to know what a specific branch carries.")]
    public async Task<string> GetBranchInventoryAsync(
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
            .Select(i => new
            {
                DrugName = i.Drug.BrandName,
                i.StockQuantity,
                i.UnitPrice
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!items.Any())
            return $"No inventory data found for branch '{branchName}', or the branch does not exist.";

        return JsonSerializer.Serialize(items, JsonOptions);
    }
}