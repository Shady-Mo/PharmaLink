using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Addresses.Requests
{

    public class CreateAddressRequestDTO
    {
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? BuildingNumber { get; set; }
        public string? FloorNumber { get; set; }
        public string? ApartmentNumber { get; set; }
        public string? AdditionalInstructions { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}
