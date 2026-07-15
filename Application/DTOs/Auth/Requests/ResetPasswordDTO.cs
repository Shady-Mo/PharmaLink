using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth.Requests
{
    public class ResetPasswordDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }
}
