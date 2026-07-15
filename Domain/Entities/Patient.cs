namespace Domain.Entities;

public class Patient : AppUser
{
    public ICollection<Address> Addresses { get; set; } = new HashSet<Address>();
    public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    public ICollection<PrescriptionReview> PrescriptionReviews { get; set; } = new HashSet<PrescriptionReview>();
}