using System;

namespace Application.DTOs.PharmacyOwner.Responses
{
    public class PharmacyOwnerDetailsDTO
    {
        public Guid PharmacyId { get; set; }
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}
