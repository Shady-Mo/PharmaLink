using Domain.Enums;

namespace Application.DTOs.Admin.Users;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public UserStatus Status { get; set; }
}
