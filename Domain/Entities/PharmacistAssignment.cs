namespace Domain.Entities;

public class PharmacistAssignment
{
    public Guid Id { get; set; }
    public Guid PharmacistId { get; set; }
    public Guid PharmacyId { get; set; }
    public Guid AssignedByPharmacyAdminId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Pharmacist Pharmacist { get; set; } = null!;
    public Pharmacy Pharmacy { get; set; } = null!;
    public PharmacyAdmin AssignedByPharmacyAdmin { get; set; } = null!;
}
