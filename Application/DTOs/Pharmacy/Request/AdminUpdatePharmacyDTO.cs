using Microsoft.AspNetCore.Http;
using Domain.Enums;

namespace Application.DTOs.Pharmacy.Request
{
    public class AdminUpdatePharmacyDTO
    {
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public IFormFile? LogoFile { get; set; }
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    }
}
