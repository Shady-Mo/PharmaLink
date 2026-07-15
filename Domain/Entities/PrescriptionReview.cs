namespace Domain.Entities;

public class PrescriptionReview
{
    public Guid PrescriptionReviewId { get; set; }

    public Guid PatientUserId { get; set; }

    public string PrescriptionImagePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string AIModel { get; set; } = string.Empty;

    public PrescriptionReviewStatus ReviewStatus { get; set; } = PrescriptionReviewStatus.PendingReview;

    public Guid? PharmacistUserId { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Guid? CreatedOrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Pharmacist? Pharmacist { get; set; }
    public Order? CreatedOrder { get; set; }

    public ICollection<PrescriptionReviewMedicine> Medicines { get; set; }
        = new HashSet<PrescriptionReviewMedicine>();
}