namespace Application.Services.PrescriptionAudit;

public interface IAlternativeSearchPlugin
{
    Task<DrugMatchResult> FindAlternativeAsync(
        ExtractedMedicineItem medicine,
        Drug? unavailableDrug,
        CancellationToken cancellationToken = default);
}
