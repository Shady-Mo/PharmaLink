
namespace Application.DTOs.Pharmacy.Responses
{
    public class GetPharmacyDTO
    {
        public Guid PharmacyId { get; set; }
        public Guid OwnerUserId { get; set; }
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
    }
}
