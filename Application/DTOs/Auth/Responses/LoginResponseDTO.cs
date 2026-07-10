namespace Application.DTOs.Auth.Responses
{
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
