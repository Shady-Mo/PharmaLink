namespace Application.DTOs.Addresses.Response
{

    public class AddressResponseDTO
    {
        public Guid AddressId { get; set; }
        public Guid UserId { set; get; }
        public string Label { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}