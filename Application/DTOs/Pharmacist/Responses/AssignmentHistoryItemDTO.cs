namespace Application.DTOs.Pharmacist.Responses;

public class AssignmentHistoryItemDTO
{
    public Guid AssignmentId { get; set; }
    public Guid PharmacistId { get; set; }
    public Guid PharmacyId { get; set; }
    public string PharmacyLegalName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public Guid AssignedByAdminId { get; set; }
    public string AssignedByAdminFullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
