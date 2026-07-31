using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Admin.Users;

public class UpdateUserRoleDto
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
