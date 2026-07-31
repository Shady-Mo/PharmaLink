using Application.DTOs.Admin.Users;
using Application.DTOs;
using Application.Common;

namespace Application.Services;

public interface IAdminUserService
{
    Task<Result<PaginatedList<AdminUserDto>>> GetUsersAsync(AdminUserFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto, Guid currentAdminId, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> UpdateUserRoleAsync(Guid userId, UpdateUserRoleDto dto, Guid currentAdminId, CancellationToken cancellationToken = default);
}
