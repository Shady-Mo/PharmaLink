namespace Application.DTOs.Pharmacy.Request
{
    public class UpdatePharmacyProfileDto
    {
        public string PharmacyName { get; set; } = string.Empty;

        public IFormFile? LogoFile { get; set; }
    }
}
