namespace Domain.Entities;

public class Pharmacist : AppUser
{
    public ICollection<PrescriptionReview> ReviewedPrescriptions { get; set; } = new HashSet<PrescriptionReview>();
    public ICollection<PharmacistAssignment> Assignments { get; set; } = new HashSet<PharmacistAssignment>();
}