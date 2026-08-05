using Application.DTOs.Supplier;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class SupplierProfileService(AppDbContext _context) : ISupplierProfileService
    {
        public async Task<Result<SupplierProfileDto>> GetProfileAsync(Guid supplierId)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == supplierId);

            if (supplier == null)
                return Result.Failure<SupplierProfileDto>(SupplierDrugErrors.NotFound);

            var dto = new SupplierProfileDto
            {
                SupplierId = supplier.Id,
                FullName = supplier.FullName,
                 Email = supplier.Email,
                 PhoneNumber = supplier.PhoneNumber,
            };

            return Result.Success(dto);
        }

        public async Task<Result> UpdateProfileAsync(Guid supplierId, UpdateSupplierProfileDto dto)
        {
            var existingByPhone = await _context.AppUsers
               .FirstOrDefaultAsync(p => p.PhoneNumber == dto.PhoneNumber && p.Id != supplierId);
            if (existingByPhone is not null)
            {
                return Result.Failure(SupplierDrugErrors.BadRequest);
            }

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);

            if (supplier == null)
                return Result.Failure(SupplierDrugErrors.NotFound);

            supplier.FullName = dto.FullName;
            supplier.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();

            return Result.Success();
        }
    }
}