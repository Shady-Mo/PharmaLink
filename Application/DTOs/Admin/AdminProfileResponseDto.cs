using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Admin
{
    public class AdminProfileResponseDto
    {
        public Guid AdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
