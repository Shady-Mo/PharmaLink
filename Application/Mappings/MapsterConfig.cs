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

        // Order Mappings
        config.NewConfig<Order, GetOrderDTO>();
        config.NewConfig<OrderItem, OrderItemResponseDTO>();

        //// Patient  moshady21
        //config.NewConfig<Address, PatientAddressDto>()
        //    .Map(dest => dest.Latitude, src => src.GeoLocation != null ? (double?)src.GeoLocation.Y : null)
        //    .Map(dest => dest.Longitude, src => src.GeoLocation != null ? (double?)src.GeoLocation.X : null);

        //config.NewConfig<Patient, PatientProfileDto>()
        //    .Map(dest => dest.PatientId, src => src.Id)
        //    .Map(dest => dest.Status, src => src.Status.ToString())
        //    .Map(dest => dest.Addresses, src => src.Addresses);
    }
}