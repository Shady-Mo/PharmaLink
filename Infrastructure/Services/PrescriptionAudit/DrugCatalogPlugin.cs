using Microsoft.SemanticKernel;

namespace Infrastructure.Services.PrescriptionAudit;

public class DrugCatalogPlugin(AppDbContext context) : IDrugCatalogPlugin
{
    private const double FuzzyThreshold = 0.72;

    [KernelFunction("find_best_catalog_match")]
    public async Task<DrugMatchResult> FindBestMatchAsync(
        ExtractedMedicineItem medicine,
        CancellationToken cancellationToken = default)
    {
        var name = medicine.MedicineName;
        var normalizedName = DrugTextNormalizer.Normalize(name);
        var drugs = await LoadCandidateDrugsAsync(medicine, cancellationToken);

        var exact = drugs
            .Where(d => DrugTextNormalizer.Normalize(d.BrandName) == normalizedName)
            .OrderByDescending(IsAvailable)
            .ThenByDescending(d => DrugTextNormalizer.SameStrength(medicine.Strength, d.Strength))
            .ThenByDescending(d => DrugTextNormalizer.SameDosageForm(medicine.DosageForm, d.Form))
            .FirstOrDefault();

        if (exact is not null)
        {
            if (IsAvailable(exact))
            {
                return new DrugMatchResult
                {
                    Status = PrescriptionMedicineMatchStatus.ExactMatch,
                    DrugId = exact.DrugId,
                    Drug = exact,
                    Score = 1,
                    Reason = "Exact brand-name catalog match."
                };
            }

            return new DrugMatchResult
            {
                Status = PrescriptionMedicineMatchStatus.Unavailable,
                DrugId = exact.DrugId,
                Drug = exact,
                Score = 1,
                Reason = "Exact catalog medicine exists but is inactive or unavailable in inventory."
            };
        }

        var fuzzy = drugs
            .Select(d => new
            {
                Drug = d,
                NameScore = GetCatalogNameScore(name, d),
                StrengthMatch = DrugTextNormalizer.SameStrength(medicine.Strength, d.Strength),
                FormMatch = DrugTextNormalizer.SameDosageForm(medicine.DosageForm, d.Form),
                Available = IsAvailable(d)
            })
            .Where(c => c.NameScore >= FuzzyThreshold)
            .OrderByDescending(c => c.Available)
            .ThenByDescending(c => c.StrengthMatch)
            .ThenByDescending(c => c.FormMatch)
            .ThenByDescending(c => c.NameScore)
            .FirstOrDefault();

        if (fuzzy is not null)
        {
            if (fuzzy.Available)
            {
                var score = fuzzy.NameScore;
                if (fuzzy.StrengthMatch) score += 0.05;
                if (fuzzy.FormMatch) score += 0.03;

                return new DrugMatchResult
                {
                    Status = PrescriptionMedicineMatchStatus.FuzzyMatch,
                    DrugId = fuzzy.Drug.DrugId,
                    Drug = fuzzy.Drug,
                    Score = Math.Min(score, 0.99),
                    Reason = "High-confidence normalized fuzzy catalog match."
                };
            }

            return new DrugMatchResult
            {
                Status = PrescriptionMedicineMatchStatus.Unavailable,
                DrugId = fuzzy.Drug.DrugId,
                Drug = fuzzy.Drug,
                Score = fuzzy.NameScore,
                Reason = "Likely catalog medicine exists but is inactive or unavailable in inventory."
            };
        }

        return new DrugMatchResult
        {
            Status = PrescriptionMedicineMatchStatus.NotFound,
            Score = 0,
            Reason = "No exact or high-confidence fuzzy catalog match was found."
        };
    }

    internal static bool IsAvailable(Drug drug)
    {
        return drug.IsActive && drug.Inventories.Any(i => i.StockQuantity - i.ReservedQuantity > 0);
    }

    private static double GetCatalogNameScore(string extractedName, Drug drug)
    {
        var directScore = Math.Max(
            DrugTextNormalizer.Similarity(extractedName, drug.BrandName),
            DrugTextNormalizer.Similarity(extractedName, drug.GenericName));

        var tokenScore = DrugTextNormalizer.TokenOverlapScore(
            extractedName,
            drug.BrandName,
            drug.GenericName,
            drug.ArabicName);

        var containsScore =
            DrugTextNormalizer.ContainsMeaningfulName(extractedName, drug.BrandName)
            || DrugTextNormalizer.ContainsMeaningfulName(extractedName, drug.GenericName)
            || DrugTextNormalizer.ContainsMeaningfulName(extractedName, drug.ArabicName)
                ? 0.9
                : 0;

        return Math.Max(directScore, Math.Max(tokenScore, containsScore));
    }

    private async Task<List<Drug>> LoadCandidateDrugsAsync(
        ExtractedMedicineItem medicine,
        CancellationToken cancellationToken)
    {
        var tokens = DrugTextNormalizer
            .MeaningfulTokens(medicine.MedicineName, medicine.GenericName)
            .Take(4)
            .ToList();

        var candidatesById = new Dictionary<Guid, Drug>();

        foreach (var token in tokens)
        {
            var likePattern = $"%{token}%";

            var tokenCandidates = await context.Drugs
                .Include(d => d.Inventories)
                .AsNoTracking()
                .Where(d =>
                    EF.Functions.Like(d.BrandName, likePattern)
                    || EF.Functions.Like(d.GenericName, likePattern)
                    || EF.Functions.Like(d.ArabicName, likePattern))
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var candidate in tokenCandidates)
            {
                candidatesById.TryAdd(candidate.DrugId, candidate);
            }
        }

        if (candidatesById.Count > 0)
        {
            return candidatesById.Values.ToList();
        }

        // Fallback keeps OCR-heavy cases working, but should be rare after token prefiltering.
        return await context.Drugs
            .Include(d => d.Inventories)
            .AsNoTracking()
            .Take(500)
            .ToListAsync(cancellationToken);
    }
}
