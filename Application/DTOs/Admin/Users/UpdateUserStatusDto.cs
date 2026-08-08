using Domain.Enums;

namespace Application.DTOs.Admin.Users;

public class UpdateUserStatusDto
{
    public UserStatus Status { get; set; }
}
