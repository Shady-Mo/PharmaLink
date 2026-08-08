using Application.DTOs.Supplier;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface ISupplierDrugService
    {
        Task<Result<(List<SupplierDrugDto> Drugs, int TotalCount)>> GetMyDrugsAsync(Guid supplierId, string? searchTerm, int pageNumber = 1, int pageSize = 10);
        Task<Result> AddDrugToMyListAsync(Guid supplierId, Guid drugId);
        Task<Result> RemoveDrugFromMyListAsync(Guid supplierId, Guid drugId);
        Task<Result<List<AvailableDrugDto>>> SearchGlobalDrugsAsync(string searchTerm);
    }
}
