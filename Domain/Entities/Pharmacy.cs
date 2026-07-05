using Domain.Enums;

namespace Domain.Entities;

public class Pharmacy {
    public Guid PharmacyID { get; set; }
    public Guid OwnerUserID { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; set; }

    public Pharmacist Owner { get; set; } = null!;
    public ICollection<PharmacyBranch> Branches { get; set; } = new HashSet<PharmacyBranch>();
}
