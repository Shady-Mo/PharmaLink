using Application.DTOs.Drug.Requests;
using Application.DTOs.Drug.Responses;
using Domain.Entities;
using Mapster;

namespace Application.Mappings;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Drug, DrugDto>();
        config.NewConfig<CreateDrugDto, Drug>();
        config.NewConfig<UpdateDrugDto, Drug>();
    }
}
