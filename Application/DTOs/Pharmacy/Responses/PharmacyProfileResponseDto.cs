namespace Application.DTOs.Pharmacy.Responses
{
    public class PharmacyProfileResponseDto
    {
        public Guid PharmacyId { get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
    }
}
