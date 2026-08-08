namespace Application.Services.AI;

public class AIExtractionResult
{
    public string ModelUsed { get; set; } = string.Empty;
    public bool IsValidPrescription { get; set; }
    public string? ValidationMessage { get; set; }
    public string? ExtractedText { get; set; }
    public string? AISummary { get; set; }
    public double? ExtractionConfidence { get; set; }
    public string? RawResponse { get; set; }
    public List<ExtractedMedicineItem> Medicines { get; set; } = [];
    public bool IsEmpty => Medicines.Count == 0;
}
