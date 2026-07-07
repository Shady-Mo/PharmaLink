namespace Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    
    public UserStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
}