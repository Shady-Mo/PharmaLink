namespace Application.Services.PrescriptionAudit;

public class DrugMatchResult
{
    public PrescriptionMedicineMatchStatus Status { get; set; } = PrescriptionMedicineMatchStatus.NotFound;
    public Guid? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public Guid? SuggestedAlternativeDrugId { get; set; }
    public Drug? SuggestedAlternativeDrug { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool RequiresPatientApproval => Status == PrescriptionMedicineMatchStatus.AlternativeSuggested;
}
