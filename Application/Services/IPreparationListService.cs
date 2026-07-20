using Application.DTOs.PreparationList.Request;
using Application.DTOs.PreparationList.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IPreparationListService
    {
        Task<Result<PaginatedList<PreparationListDTO>>> GetPreparationListByPharmacistId(Guid id, PreparationListQueryParameters parameters);
    }
}
