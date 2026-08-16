using Application.DTOs.Admin.Users;

namespace Infrastructure.Services;

public class AdminUserService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IWebPushNotificationService pushNotificationService,
    AppDbContext context)
    : IAdminUserService
{
    private static readonly HashSet<string> SupportedRoles =
    [
        AppRoles.Patient,
        AppRoles.Pharmacist,
        AppRoles.PrescriptionReviewTeam,
        AppRoles.PharmacyAdmin,
        AppRoles.Admin,
        AppRoles.Supplier,
        AppRoles.DeliveryDriver
    ];

    public async Task<Result<PaginatedList<AdminUserDto>>> GetUsersAsync(AdminUserFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.Contains(search) || (u.Email != null &&
                    u.Email.Contains(search)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(u => u.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(filter.Role);
            var userIds = usersInRole.Select(u => u.Id).ToList();
            query = query.Where(u => userIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(filter.SortBy))
        {
            var isDescending = filter.SortDirection?.ToLower() == "desc";
            query = filter.SortBy.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                "email" => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "date" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(u => u.CreatedAt);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtoList = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Unknown";

            dtoList.Add(new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = role,
                RegistrationDate = user.CreatedAt,
                Status = (int)user.Status == 0 ? UserStatus.Active : user.Status
            });
        }

        var paginatedList = new PaginatedList<AdminUserDto>(dtoList, filter.PageNumber, totalCount, filter.PageSize);
        return Result.Success(paginatedList);
    }

    public async Task<Result> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto, Guid currentAdminId,
        CancellationToken cancellationToken = default)
    {
        if (userId == currentAdminId)
        {
            return Result.Failure(AdminUserErrors.CannotDeactivateSelf);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(AdminUserErrors.NotFound);
        }

        user.Status = dto.Status;
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return Result.Failure(AdminUserErrors.UpdateFailed);
        }

        var title = "";
        var message = "";
        switch (dto.Status)
        {
            case UserStatus.Active:
                title = "تم تفعيل حسابك ✅";
                message = "تم تفعيل حسابك ويمكنك الآن استخدام جميع خدمات فارما لينك.";
                break;
            case UserStatus.Suspended:
                title = "تم إيقاف حسابك ⚠️";
                message = "تم إيقاف حسابك مؤقتاً. يرجى التواصل مع الإدارة للاستفسار.";
                break;
            case UserStatus.Inactive:
                title = "تم إلغاء حسابك ❌";
                message = "تم إلغاء حسابك. يرجى التواصل مع الإدارة للاستفسار.";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!string.IsNullOrEmpty(title))
        {
            await pushNotificationService.SendNotificationAsync(userId, title, message);
        }

        return Result.Success();
    }

    public async Task<Result<AdminUserDto>> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleDto dto,
        Guid currentAdminId,
        CancellationToken cancellationToken = default)
    {
        if (userId == currentAdminId)
        {
            return Result.Failure<AdminUserDto>(AdminUserErrors.CannotChangeOwnRole);
        }

        var selectedRole =
            SupportedRoles.FirstOrDefault(role => role.Equals(dto.Role?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (selectedRole is null)
        {
            return Result.Failure<AdminUserDto>(AdminUserErrors.InvalidRole);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return Result.Failure<AdminUserDto>(AdminUserErrors.NotFound);
        }

        if (!await roleManager.RoleExistsAsync(selectedRole))
        {
            return Result.Failure<AdminUserDto>(AdminUserErrors.InvalidRole);
        }

        var currentRoles = await userManager.GetRolesAsync(user);

        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded)
        {
            return Result.Failure<AdminUserDto>(AdminUserErrors.RoleUpdateFailed);
        }


        var addResult = await userManager.AddToRoleAsync(user, selectedRole);

        if (!addResult.Succeeded)
        {
            return Result.Failure<AdminUserDto>(AdminUserErrors.RoleUpdateFailed);
        }

        await context.Users.Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => EF.Property<string>(a, "UserType"), selectedRole),
                cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(user, selectedRole));
    }

    private static AdminUserDto ToDto(AppUser user, string role) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber ?? string.Empty,
        Role = role,
        RegistrationDate = user.CreatedAt,
        Status = (int)user.Status == 0 ? UserStatus.Active : user.Status
    };
}