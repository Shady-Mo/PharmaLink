namespace Domain.Entities;

public class Pharmacy
{
    public Guid PharmacyId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    
    public VerificationStatus VerificationStatus { get; set; }

    public ICollection<PharmacyAdmin> Admins { get; set; } = new HashSet<PharmacyAdmin>();
    public ICollection<PharmacyBranch> Branches { get; set; } = new HashSet<PharmacyBranch>();
}