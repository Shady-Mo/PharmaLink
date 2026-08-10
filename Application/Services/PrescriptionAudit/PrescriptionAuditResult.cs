namespace Application.Services.PrescriptionAudit;

public class PrescriptionAuditResult
{
    public bool IsValidPrescription { get; set; }
    public string? ValidationMessage { get; set; }
    public Domain.Entities.PrescriptionReview? Review { get; set; }
    public Guid? CartId { get; set; }
    public IReadOnlyList<PrescriptionReviewMedicine> Medicines { get; set; } = [];
}
