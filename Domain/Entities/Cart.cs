namespace Domain.Entities;

public class Cart
{
    public Guid CartId { get; set; }

    public Guid PatientUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


    public Patient Patient { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new HashSet<CartItem>();
}
