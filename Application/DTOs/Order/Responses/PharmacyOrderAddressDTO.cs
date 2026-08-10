namespace Application.DTOs.Order.Responses;

public class PharmacyOrderAddressDTO
{
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? BuildingNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? AdditionalInstructions { get; set; }
}
