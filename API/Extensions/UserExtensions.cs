namespace API.Extensions;

public static class UserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtClaimTypes.UserId), out var id) ? id : Guid.Empty;

    public static string? GetRoleName(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtClaimTypes.RoleName) ?? user.FindFirstValue(ClaimTypes.Role);

    public static List<Guid> GetBranchIds(this ClaimsPrincipal user) =>
        user.FindAll(JwtClaimTypes.BranchId)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
}