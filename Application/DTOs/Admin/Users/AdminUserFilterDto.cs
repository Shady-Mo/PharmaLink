using Application.DTOs;
using Domain.Enums;

namespace Application.DTOs.Admin.Users;

public class AdminUserFilterDto : PaginatedRequest
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public UserStatus? Status { get; set; }
}
