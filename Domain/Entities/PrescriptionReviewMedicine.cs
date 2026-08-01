namespace Domain.Entities;

public class PrescriptionReviewMedicine
{
    public Guid PrescriptionReviewMedicineId { get; set; }

    public Guid PrescriptionReviewId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public string? OriginalMedicineName { get; set; }

    public string? GenericName { get; set; }

    public string? Strength { get; set; }

    public string? DosageForm { get; set; }

    public string? Dose { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public int Quantity { get; set; } = 1;

    public string? Route { get; set; }

    public double? Confidence { get; set; }

    public Guid? MatchedDrugId { get; set; }

    public Guid? SuggestedAlternativeDrugId { get; set; }

    public PrescriptionMedicineMatchStatus MatchStatus { get; set; } =
        PrescriptionMedicineMatchStatus.NotFound;

    public string? MatchReason { get; set; }

    public double? MatchScore { get; set; }

    public bool RequiresPatientApproval { get; set; }

    public DateTime? PatientApprovedAt { get; set; }

    public bool IsEdited { get; set; } = false;

    // Navigation property
    public PrescriptionReview PrescriptionReview { get; set; } = null!;
    public Drug? MatchedDrug { get; set; }
    public Drug? SuggestedAlternativeDrug { get; set; }
}
