namespace Application.DTOs.Pharmacy.Request
{
    public class AddPharmacyDTO
    {
        public Guid OwnerUserId { get; set; }
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}
