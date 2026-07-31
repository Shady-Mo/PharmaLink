namespace Application.DTOs.AI;

public class ExtractPrescriptionResponseDTO
{
    public bool IsValidPrescription { get; set; }
    public string? ValidationMessage { get; set; }
    public string? ExtractedText { get; set; }
    public string? AiSummary { get; set; }
    public double? ExtractionConfidence { get; set; }
    public List<ExtractedPrescriptionMedicineDTO> Medicines { get; set; } = [];
}
