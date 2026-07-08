using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth.Responses
{
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
