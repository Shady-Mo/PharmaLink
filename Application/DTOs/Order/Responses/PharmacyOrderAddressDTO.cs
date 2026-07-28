namespace Application.DTOs.Order.Responses;

public class PharmacyOrderAddressDTO
{
    public string AddressLine { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Governorate { get; set; } = string.Empty;
}
