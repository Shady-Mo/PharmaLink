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
    }
}