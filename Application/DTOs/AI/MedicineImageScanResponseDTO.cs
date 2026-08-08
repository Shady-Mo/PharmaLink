namespace Application.DTOs.AI;

public class MedicineImageScanResponseDTO
{
    public MedicineImageExtractionResponseDTO Extraction { get; set; } = new();
    public Guid? MatchedDrugId { get; set; }
    public Guid? SuggestedAlternativeDrugId { get; set; }
    public Guid? CartDrugId { get; set; }
    public string MatchStatus { get; set; } = "NotFound";
    public double MatchScore { get; set; }
    public string? MatchReason { get; set; }
    public bool CanBeAddedToCart { get; set; }
}
