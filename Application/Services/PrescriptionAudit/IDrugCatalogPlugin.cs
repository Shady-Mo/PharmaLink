namespace Application.Services.PrescriptionAudit;

public interface IDrugCatalogPlugin
{
    Task<DrugMatchResult> FindBestMatchAsync(
        ExtractedMedicineItem medicine,
        CancellationToken cancellationToken = default);
}
