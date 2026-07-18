namespace Application.Services;

public interface IJwtTokenGeneratorService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(IEnumerable<Claim> claims);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}