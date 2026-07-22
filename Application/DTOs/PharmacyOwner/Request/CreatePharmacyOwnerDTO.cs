using System;

namespace Application.DTOs.PharmacyOwner.Request
{
    public class CreatePharmacyOwnerDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid PharmacyId { get; set; }
    }
}
