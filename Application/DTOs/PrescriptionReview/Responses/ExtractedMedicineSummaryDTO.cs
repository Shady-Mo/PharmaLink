using System;

namespace Application.DTOs.PrescriptionReview.Responses;

public class ExtractedMedicineSummaryDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalName
    {
        get => Name;
        set => Name = value;
    }

    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public int Quantity { get; set; }
    public double? Confidence { get; set; }
    public Guid? MatchedDrugId { get; set; }
    public Guid? SuggestedAlternativeDrugId { get; set; }
    public string Status { get; set; } = "NotFound";
    public string? AiNote { get; set; }
    public bool RequiresPatientApproval { get; set; }
}
