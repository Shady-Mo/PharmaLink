using Application.DTOs.Drug;
using Application.Services;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class DrugService : IDrugService
    {
        private readonly AppDbContext context;
        private readonly ILogger<DrugService> logger;

        public DrugService(AppDbContext context, ILogger<DrugService> logger)
        {
            this.context = context;
            this.logger = logger;
        }
        public async Task SeedDrugsAsync(string jsonFilePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(jsonFilePath))
            {
                logger.LogError($"Seed file not found at path: {jsonFilePath}");
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

            var data = JsonSerializer.Deserialize<DrugSeedRoot>(jsonContent);

            if (data?.Data is null || data.Data.Count == 0)
            {
                logger.LogWarning("No data found in the JSON file");
                return;
            }

            var ngCodes = new HashSet<string>(
                await context.Drugs.Select(d => d.NdcCode).ToListAsync()
                );
            foreach (var item in data.Data)
            {

                if (string.IsNullOrEmpty(item.Barcode) || ngCodes.Contains(item.Barcode))
                {
                    logger.LogWarning($"Duplicate detected: Drug with NdcCode/Barcode '{item.Barcode}' already exists.");
                    continue;
                }

                var drug = new Drug
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
                };

                context.Drugs.Add(drug);
                ngCodes.Add(drug.NdcCode);
                
            }

            var count = await context.SaveChangesAsync();

            if(count==0)
            {
                logger.LogInformation("Catalog is already up-to-date. No new drugs were seeded.");
            }

        }
    }
}
