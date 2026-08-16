using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seeders;

public class InventorySeeder(
    AppDbContext context,
    ILogger<InventorySeeder> logger)
{
    private readonly string[] _majorChains = { "العزبي", "رشدي", "سيف", "الطرشوبي", "عز الدين", "19011", "دوائي", "نورماندي", "مصر", "خليل" };

    public async Task SeedAsync()
    {
        var branches = await context.PharmacyBranches
            .Include(b => b.Pharmacy)
            .Where(b => !context.PharmacyInventories.Any(i => i.BranchId == b.BranchId))
            .AsNoTracking()
            .ToListAsync();

        if (branches.Count == 0)
        {
            logger.LogWarning("No pharmacy branches found. Please run OsmPharmacySeeder first.");
            return;
        }

        var drugs = await context.Drugs
            .AsNoTracking()
            .Select(d => new { d.DrugId, d.Price })
            .ToListAsync();

        if (drugs.Count == 0)
        {
            logger.LogWarning("No drugs found in the database. Inventory seeder cannot run.");
            return;
        }

        logger.LogInformation("Starting Inventory Seeding (Smart Mapping) for {BranchCount} branches and {DrugCount} drugs...", branches.Count, drugs.Count);

        var random = new Random();
        int totalInventoriesAdded = 0;
        
        var fullCatalog = drugs.ToList();
        var coreCatalogSize = Math.Min(300, drugs.Count);
        
        var inventoriesToInsert = new List<PharmacyInventory>();

        foreach (var branch in branches)
        {
            var isChain = _majorChains.Any(c => branch.Pharmacy.LegalName.Contains(c, StringComparison.OrdinalIgnoreCase));
            
            var catalogToUse = fullCatalog;
            
            if (!isChain)
            {
                catalogToUse = drugs.OrderBy(x => random.Next()).Take(coreCatalogSize).ToList();
            }
            else
            {
                int chainCatalogSize = (int)(drugs.Count * 0.9);
                catalogToUse = drugs.OrderBy(x => random.Next()).Take(chainCatalogSize).ToList();
            }

            foreach (var drug in catalogToUse)
            {
                int stock = isChain ? random.Next(10, 100) : random.Next(2, 30);
                
                int daysToExpiry = random.Next(180, 1095);
                var expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysToExpiry));

                int reorderPoint = random.Next(1, 10);

                var inventory = new PharmacyInventory
                {
                    InventoryId = Guid.NewGuid(),
                    BranchId = branch.BranchId,
                    DrugId = drug.DrugId,
                    StockQuantity = stock,
                    ReservedQuantity = 0,
                    UnitPrice = drug.Price > 0 ? drug.Price : (decimal)(random.Next(10, 500)),
                    ExpiryDate = expiryDate,
                    LastSyncedAt = DateTime.UtcNow,
                    ReorderPoint = reorderPoint
                };

                inventoriesToInsert.Add(inventory);
                totalInventoriesAdded++;
            }

            if (inventoriesToInsert.Count >= 5000)
            {
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                await context.PharmacyInventories.AddRangeAsync(inventoriesToInsert);
                await context.SaveChangesAsync();
                logger.LogInformation("Inserted batch of {BatchSize} inventory items. Total: {Total}", inventoriesToInsert.Count, totalInventoriesAdded);
                inventoriesToInsert.Clear();
                
                context.ChangeTracker.Clear();
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }

        if (inventoriesToInsert.Count > 0)
        {
            await context.PharmacyInventories.AddRangeAsync(inventoriesToInsert);
            await context.SaveChangesAsync();
            logger.LogInformation("Inserted final batch of {BatchSize} inventory items. Total: {Total}", inventoriesToInsert.Count, totalInventoriesAdded);
        }

        logger.LogInformation("Successfully completed Inventory Seeding. Added {Total} inventory items.", totalInventoriesAdded);
    }
}
