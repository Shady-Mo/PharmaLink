namespace Infrastructure.Persistence.Seeders;

public class DrugSeeder(AppDbContext context, ILogger<DrugSeeder> logger)
{
    public async Task SeedAsync(string jsonFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(jsonFilePath))
        {
            logger.LogWarning("Drug dataset not found at {Path}. Skipping seeding.", jsonFilePath);
            return;
        }

        logger.LogInformation("Starting drug dataset seeding from {Path}", jsonFilePath);

        try
        {
            await using var stream = File.OpenRead(jsonFilePath);
            var records = await JsonSerializer.DeserializeAsync<List<EgyptianDrugRecord>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (records is null || records.Count == 0)
            {
                logger.LogWarning("No records found in the dataset.");
                return;
            }

            var existingBrands = await context.Drugs
                .Select(d => d.BrandName)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

            var newDrugs = new List<Drug>();
            var skippedCount = 0;
            var invalidCount = 0;

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.CommercialNameEn))
                {
                    invalidCount++;
                    continue;
                }

                if (existingBrands.Contains(record.CommercialNameEn))
                {
                    skippedCount++;
                    continue;
                }

                newDrugs.Add(new Drug
                {
                    DrugId = Guid.NewGuid(),
                    BrandName = record.CommercialNameEn ?? string.Empty,
                    GenericName = record.ScientificName ?? string.Empty,
                    Manufacturer = record.Manufacturer ?? string.Empty,
                    ArabicName = record.CommercialNameAr ?? string.Empty,
                    DrugClass = record.DrugClass ?? string.Empty,
                    Category = DrugCategoryMapper.Map(record.DrugClass, record.ScientificName),
                    Form = record.Route ?? string.Empty,
                    Price = record.PriceEgp ?? 0,
                    IsActive = true,
                    RequiresPrescription = false,
                    NdcCode = string.Empty,
                    RxNormCui = string.Empty,
                    DrugBankId = string.Empty,
                    Strength = string.Empty
                });

                existingBrands.Add(record.CommercialNameEn);
            }

            if (newDrugs.Count > 0)
            {
                logger.LogInformation("Importing {Count} new drugs...", newDrugs.Count);

                await context.Drugs.AddRangeAsync(newDrugs, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Successfully inserted {Count} drugs.", newDrugs.Count);
            }

            logger.LogInformation("Seeding completed. Skipped {Skipped} duplicates. Found {Invalid} invalid records.",
                skippedCount, invalidCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the drug dataset.");
        }
    }

    private class EgyptianDrugRecord
    {
        [JsonPropertyName("commercial_name_en")]
        public string? CommercialNameEn { get; set; }

        [JsonPropertyName("commercial_name_ar")]
        public string? CommercialNameAr { get; set; }

        [JsonPropertyName("scientific_name")] public string? ScientificName { get; set; }

        [JsonPropertyName("manufacturer")] public string? Manufacturer { get; set; }

        [JsonPropertyName("drug_class")] public string? DrugClass { get; set; }

        [JsonPropertyName("route")] public string? Route { get; set; }

        [JsonPropertyName("price_egp")] public decimal? PriceEgp { get; set; }
    }
}