namespace Domain.Entities;

public class Pharmacist : AppUser
{
    public ICollection<Pharmacy> AdministeredPharmacies { get; set; } = new HashSet<Pharmacy>();
    public ICollection<PrescriptionReview> ReviewedPrescriptions { get; set; } = new HashSet<PrescriptionReview>();
}