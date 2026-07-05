namespace Domain.Entities;

public class Pharmacist : AppUser {
    public ICollection<Pharmacy> AdministeredPharmacies { get; set; } = new HashSet<Pharmacy>();
}
