using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using Application.DTOs.PharmacyInventory.Response;

namespace Application.Mappings;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Drug, DrugDto>();
        config.NewConfig<CreateDrugDto, Drug>();
        config.NewConfig<UpdateDrugDto, Drug>();
        config.NewConfig<RegisterRequestDTO, Patient>()
            .Map(dest => dest.UserName, src => src.Email.ToLowerInvariant())
            .Map(dest => dest.Email, src => src.Email.ToLowerInvariant());

        config.NewConfig<PharmacyInventory, GetPharmacyInventoryDTO>()
            .Map(dest => dest.DrugName, src => src.Drug.BrandName);
        // Order Mappings
        config.NewConfig<Order, GetOrderDTO>();
        config.NewConfig<OrderItem, OrderItemResponseDTO>();
        config.NewConfig<Address, CreateAddressRequestDTO>();
        config.NewConfig<Address, AddressResponseDTO>()
            .Map(dest => dest.Longitude, src => src.GeoLocation != null ? src.GeoLocation.X : 0)
            .Map(dest => dest.Latitude, src => src.GeoLocation != null ? src.GeoLocation.Y : 0);
    }
}