using Microsoft.SemanticKernel;

namespace Infrastructure.Services.PrescriptionAudit;

public class AlternativeSearchPlugin(AppDbContext context) : IAlternativeSearchPlugin
{
    [KernelFunction("find_same_ingredient_alternative")]
    public async Task<DrugMatchResult> FindAlternativeAsync(
        ExtractedMedicineItem medicine,
        Drug? unavailableDrug,
        CancellationToken cancellationToken = default)
    {
        var genericName = unavailableDrug?.GenericName ?? medicine.GenericName;

        if (string.IsNullOrWhiteSpace(genericName))
        {
            return new DrugMatchResult
            {
                Status = PrescriptionMedicineMatchStatus.NotFound,
                Reason = "Cannot suggest an alternative without an active ingredient.",
                Score = 0
            };
        }

        var genericKey = DrugTextNormalizer.Normalize(genericName);

        var alternativesQuery = context.Drugs
            .Include(d => d.Inventories)
            .Where(d => EF.Functions.Like(d.GenericName, $"%{genericName}%"))
            .AsNoTracking();

        if (unavailableDrug is not null)
        {
            alternativesQuery = alternativesQuery.Where(d => d.DrugId != unavailableDrug.DrugId);
        }

        var alternatives = await alternativesQuery
            .Take(100)
            .ToListAsync(cancellationToken);

        var best = alternatives
            .Where(d => DrugTextNormalizer.Normalize(d.GenericName) == genericKey)
            .Where(DrugCatalogPlugin.IsAvailable)
            .Select(d => new
            {
                Drug = d,
                StrengthMatch = DrugTextNormalizer.SameStrength(medicine.Strength ?? unavailableDrug?.Strength, d.Strength),
                FormMatch = DrugTextNormalizer.SameDosageForm(medicine.DosageForm ?? unavailableDrug?.Form, d.Form)
            })
            .OrderByDescending(a => a.StrengthMatch)
            .ThenByDescending(a => a.FormMatch)
            .ThenBy(a => a.Drug.Price)
            .FirstOrDefault();

        if (best is null)
        {
            return new DrugMatchResult
            {
                Status = PrescriptionMedicineMatchStatus.NotFound,
                Reason = "No available medicine with the same active ingredient was found.",
                Score = 0
            };
        }

        var score = 0.82;
        if (best.StrengthMatch) score += 0.1;
        if (best.FormMatch) score += 0.05;

        return new DrugMatchResult
        {
            Status = PrescriptionMedicineMatchStatus.AlternativeSuggested,
            DrugId = unavailableDrug?.DrugId,
            Drug = unavailableDrug,
            SuggestedAlternativeDrugId = best.Drug.DrugId,
            SuggestedAlternativeDrug = best.Drug,
            Score = Math.Min(score, 0.97),
            Reason = "Original medicine is unavailable or not found. Suggested equivalent based on the same active ingredient."
        };
    }
}
