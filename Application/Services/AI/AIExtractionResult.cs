namespace Application.Services.AI;

public class AIExtractionResult
{
    public string ModelUsed { get; set; } = string.Empty;
    public List<ExtractedMedicineItem> Medicines { get; set; } = [];
    public bool IsEmpty => Medicines.Count == 0;
}