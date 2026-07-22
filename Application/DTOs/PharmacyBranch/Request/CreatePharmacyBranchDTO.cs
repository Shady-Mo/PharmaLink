namespace Application.DTOs.PharmacyBranch.Request
{
    public class CreatePharmacyBranchDTO
    {
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string WorkingHours { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal ServiceRadiusKm { get; set; }
        public bool SupportsDelivery { get; set; }
        public bool SupportsPickup { get; set; }
    }
}
