using System;
using System.Security.Claims;
using Application.Abstractions;
using Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? PatientId
    {
        get
        {
            var userIdStr = httpContextAccessor.HttpContext?.User?.FindFirst(JwtClaimTypes.UserId)?.Value;

            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
