using System;

namespace Application.DTOs.PrescriptionReview.Responses;

public class MedicineDetailDTO
{
    public Guid Id { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? OriginalMedicineName { get; set; }
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Dose { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public int Quantity { get; set; }
    public string? Route { get; set; }
    public double? Confidence { get; set; }
    public Guid? MatchedDrugId { get; set; }
    public Guid? SuggestedAlternativeDrugId { get; set; }
    public string MatchStatus { get; set; } = "NotFound";
    public string? MatchReason { get; set; }
    public double? MatchScore { get; set; }
    public bool RequiresPatientApproval { get; set; }
    public DateTime? PatientApprovedAt { get; set; }
    public bool IsEdited { get; set; }
}
