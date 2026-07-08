using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Application.Services
{
    public interface IJwtTokenGeneratorService
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(IEnumerable<Claim> claims);
    }
}
