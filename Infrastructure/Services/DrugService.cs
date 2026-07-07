namespace Infrastructure.Services;

public class DrugService(AppDbContext context, ILogger<DrugService> logger) : IDrugService
{
    public async Task SeedDrugsAsync(
        string jsonFilePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(jsonFilePath))
        {
            logger.LogError("Seed file not found at path: {Path}", jsonFilePath);
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

        DrugSeedRoot? data;

        try
        {
            data = JsonSerializer.Deserialize<DrugSeedRoot>(jsonContent);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize the drug seed file.");
            return;
        }

        if (data?.Data is null || !data.Data.Any())
        {
            logger.LogWarning("No data found in the JSON seed file.");
            return;
        }

        var ndcCodes = new HashSet<string>(
            await context.Drugs
                .AsNoTracking()
                .Select(d => d.NdcCode)
                .ToListAsync(cancellationToken));

        var drugsToAdd = new List<Drug>();

        foreach (var item in data.Data)
        {
            if (string.IsNullOrWhiteSpace(item.Barcode) || ndcCodes.Contains(item.Barcode))
            {
                logger.LogWarning(
                    "Duplicate detected. Drug with NdcCode '{NdcCode}' already exists.",
                    item.Barcode);

                continue;
            }

            drugsToAdd.Add(new Drug
            {
                DrugID = Guid.NewGuid(),
                BrandName = item.Name,
                GenericName = item.ActiveIngredient,
                Form = item.DosageForm,
                NdcCode = item.Barcode,
                IsActive = true,
                DrugBankID = "NF",
                RxNormCUI = "NF",
                Strength = "NF",
                RequiresPrescription = false
            });

            ndcCodes.Add(item.Barcode);
        }

        if (!drugsToAdd.Any())
        {
            logger.LogInformation("Catalog is already up to date. No new drugs were seeded.");
            return;
        }

        await context.Drugs.AddRangeAsync(drugsToAdd, cancellationToken);

        var insertedCount = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} drug(s) were successfully seeded into the catalog.",
            insertedCount);
    }
}