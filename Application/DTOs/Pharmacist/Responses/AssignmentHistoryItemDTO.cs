namespace Application.DTOs.Pharmacist.Responses;

public class AssignmentHistoryItemDTO
{
    public Guid AssignmentId { get; set; }
    public Guid PharmacyId { get; set; }
    public string PharmacyLegalName { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public Guid AssignedByPharmacyAdminId { get; set; }
}
