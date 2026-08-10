using Application.DTOs.Supplier;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class SupplierDrugService(AppDbContext _context) : ISupplierDrugService
    {
        // غير السطر بتاع GetMyDrugsAsync ليكون بالشكل ده:
        public async Task<Result<(List<SupplierDrugDto> Drugs, int TotalCount)>> GetMyDrugsAsync(Guid supplierId, string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.SupplierDrugs
                .Include(sd => sd.Drug)
                .Where(sd => sd.SupplierId == supplierId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(sd => sd.Drug.BrandName.ToLower().Contains(lowerSearchTerm));
            }

            var totalCount = await query.CountAsync();

            var drugs = await query
                .OrderBy(sd => sd.Drug.BrandName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(sd => new SupplierDrugDto
                {
                    DrugId = sd.DrugId,
                    BrandName = sd.Drug.BrandName,
                })
                .ToListAsync();

            return Result.Success((drugs, totalCount));
        }

        public async Task<Result> AddDrugToMyListAsync(Guid supplierId, Guid drugId)
        {
            var exists = await _context.SupplierDrugs
                .AnyAsync(sd => sd.SupplierId == supplierId && sd.DrugId == drugId);

            if (exists)
                return Result.Failure(SupplierDrugErrors.NotFound);

            var supplierDrug = new SupplierDrug
            {
                SupplierId = supplierId,
                DrugId = drugId
            };

            _context.SupplierDrugs.Add(supplierDrug);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> RemoveDrugFromMyListAsync(Guid supplierId, Guid drugId)
        {
            var supplierDrug = await _context.SupplierDrugs
                .FirstOrDefaultAsync(sd => sd.SupplierId == supplierId && sd.DrugId == drugId);

            if (supplierDrug == null)
                return Result.Failure(SupplierDrugErrors.NotFound);

            _context.SupplierDrugs.Remove(supplierDrug);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<AvailableDrugDto>>> SearchGlobalDrugsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Result.Success(new List<AvailableDrugDto>());
            }

            var lowerSearchTerm = searchTerm.ToLower();
            var drugs = await _context.Drugs
                .Where(d => d.BrandName.Contains(searchTerm))
                .Take(20)
                .Select(d => new AvailableDrugDto
                {
                    DrugId = d.DrugId,
                    BrandName = d.BrandName
                })
                .ToListAsync();

            return Result.Success(drugs);
        }
    }
}