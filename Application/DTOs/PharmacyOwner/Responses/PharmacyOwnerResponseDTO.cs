using System;

namespace Application.DTOs.PharmacyOwner.Responses
{
    public class PharmacyOwnerResponseDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid? PharmacyId { get; set; }
        public bool? IsSuperAdmin { get; set; }
        public PharmacyOwnerDetailsDTO? Pharmacy { get; set; }
    }
}
