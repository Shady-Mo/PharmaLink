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
        var searchKey = medicine.GenericName ?? medicine.MedicineName;

        if (string.IsNullOrWhiteSpace(searchKey) && unavailableDrug?.CategoryId == null)
        {
            return new DrugMatchResult
            {
                Status = PrescriptionMedicineMatchStatus.NotFound,
                Reason = "Cannot suggest an alternative without an active ingredient or category.",
                Score = 0
            };
        }

        var genericKey = DrugTextNormalizer.Normalize(searchKey);

        IQueryable<Drug> alternativesQuery = context.Drugs.AsNoTracking();

        if (unavailableDrug?.CategoryId != null)
        {
            alternativesQuery = alternativesQuery.Where(d => d.CategoryId == unavailableDrug.CategoryId && d.DrugId != unavailableDrug.DrugId);
        }
        else
        {
            alternativesQuery = alternativesQuery.Where(d => 
                EF.Functions.Like(d.MetaDescriptionEn, $"%{searchKey}%") || 
                EF.Functions.Like(d.MetaDescriptionAr, $"%{searchKey}%") ||
                EF.Functions.Like(d.BrandName, $"%{searchKey}%") ||
                EF.Functions.Like(d.ArabicName, $"%{searchKey}%") ||
                EF.Functions.Like(d.MetaKeywordsEn, $"%{searchKey}%") ||
                EF.Functions.Like(d.MetaKeywordsAr, $"%{searchKey}%"));
        }

        var alternatives = await alternativesQuery
            .Take(100)
            .ToListAsync(cancellationToken);

        var best = alternatives
            .Where(DrugCatalogPlugin.IsAvailable)
            .Where(d => DrugTextNormalizer.ContainsStrength(
                medicine.Strength,
                d.BrandName,
                d.ArabicName,
                d.MetaDescriptionAr,
                d.MetaDescriptionEn))
            .Select(d => new
            {
                Drug = d,
                FormMatch = DrugTextNormalizer.ContainsDosageForm(
                    medicine.DosageForm ?? unavailableDrug?.Form, 
                    d.BrandName, 
                    d.ArabicName, 
                    d.MetaDescriptionAr, 
                    d.MetaDescriptionEn, 
                    d.Form)
            })
            .OrderByDescending(a => a.FormMatch)
            .ThenBy(a => a.Drug.Price)
            .FirstOrDefault();

        if (best is null)
        {
            return new DrugMatchResult
            {
                Status = PrescriptionMedicineMatchStatus.NotFound,
                Reason = "No available medicine with the same active ingredient and strength was found.",
                Score = 0
            };
        }

        var score = 0.92;
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
