namespace Application.DTOs.Pharmacist.Responses;

public class PharmacistSummaryDTO
{
    public Guid PharmacistId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public string? ActiveBranchName { get; set; }
    public IEnumerable<AssignmentDTO>? Assignments { set; get; }
}

public class AssignmentDTO
{
    public Guid PharmacistId { get; set; }
    public Guid PharmacyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid AssignedByPharmacyAdminId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
}