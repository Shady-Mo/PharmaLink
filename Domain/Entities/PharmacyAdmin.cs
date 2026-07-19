namespace Domain.Entities;

public class PharmacyAdmin : AppUser
{
    public bool? IsSuperAdmin { get; set; }

    public Guid? PharmacyId { get; set; }

    public Pharmacy? Pharmacy { get; set; }
}
