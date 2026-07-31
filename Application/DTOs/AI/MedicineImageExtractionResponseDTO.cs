namespace Application.DTOs.AI;

public class MedicineImageExtractionResponseDTO
{
    public string MedicineName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Manufacturer { get; set; }
    public double? Confidence { get; set; }
    public string? RawText { get; set; }
}
