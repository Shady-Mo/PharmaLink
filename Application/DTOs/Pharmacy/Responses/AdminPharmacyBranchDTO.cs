using System;

namespace Application.DTOs.Pharmacy.Responses
{
    public class AdminPharmacyBranchDTO
    {
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal ServiceRadiusKm { get; set; }
        public bool SupportsDelivery { get; set; }
        public bool SupportsPickup { get; set; }
    }
}
