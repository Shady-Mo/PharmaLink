using Application.DTOs.PreparationList.Request;
using Application.DTOs.PreparationList.Response;

namespace Infrastructure.Services
{
    public class PreparationListService(AppDbContext context)
        : IPreparationListService
    {
        public async Task<Result<PaginatedList<PreparationListDTO>>> GetPreparationListByPharmacistId(Guid id, PreparationListQueryParameters parameters)
        {

            var query = context.OrderFulfillmentLegs
                .AsNoTracking()
                .Where(p => p.Branch.Pharmacy.PharmacistAssignments
                .Any(pa => pa.PharmacistId == id && pa.IsActive) 
                && p.LegStatus != LegStatus.Delivered
                && p.LegStatus != LegStatus.OutForDelivery
                && p.LegStatus != LegStatus.Cancelled && p.Order.OrderStatus != OrderStatus.Cancelled)
                .AsQueryable();

            if (parameters.Status.HasValue)
            {
                query = query.Where(p => p.LegStatus == parameters.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Order.Patient.FullName.ToLower().Contains(searchTerm) ||
                    p.OrderId.ToString().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var preparations = await query
                .OrderByDescending(p => p.OrderId)
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(p => new PreparationListDTO
                {
                    LegId = p.LegId,
                    OrderNumber = p.OrderId,
                    PatientName = p.Order.Patient.FullName,
                    Status = p.LegStatus,
                    LegType = (byte)p.LegType,
                    MedcineDTOs = p.Branch.SuppliedOrderItems
                        .Where(oi => oi.OrderId == p.OrderId && oi.BranchId == p.BranchId)
                        .Select(oi => new MedcineDTO
                        {
                            Name = oi.Drug.BrandName,
                            Quantity = oi.QuantityNeeded
                        }).ToList()
                }).ToListAsync();

            var pagedResponse = new PaginatedList<PreparationListDTO>(preparations, parameters.PageNumber, totalCount, parameters.PageSize);

            return Result.Success(pagedResponse);
        }
    }
}
