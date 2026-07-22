using System;
using Domain.Enums;

namespace Application.DTOs.PharmacyOwner.Request
{
    public class UpdatePharmacyOwnerDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid? PharmacyId { get; set; }
        public UserStatus Status { get; set; }
        public string? Password { get; set; }
    }
}
