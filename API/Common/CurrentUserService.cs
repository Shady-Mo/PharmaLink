using Domain.Constants;
using System.Security.Claims;

namespace Infrastructure.Common
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public Guid UserId
        {
            get
            {
                var value = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtClaimTypes.UserId);
                return Guid.TryParse(value, out var id) ? id : Guid.Empty;
            }
        }

        public string? RoleName =>
            httpContextAccessor.HttpContext?.User.FindFirstValue(JwtClaimTypes.RoleName);
    }
}