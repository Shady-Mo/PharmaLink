namespace Infrastructure.Services;

public class DrugService(AppDbContext context, IGeoLookupService geoLookupService) : IDrugService
{
    // Available stock (StockQuantity - ReservedQuantity) below this is "Low Stock" rather than "In Stock".
    private const int LowStockThreshold = 10;

    public async Task<Result<PaginatedList<DrugDto>>> SearchCatalogAsync(
        DrugSearchRequest filters,
        CancellationToken cancellationToken = default)
    {
        var query = context.Drugs.AsNoTracking().Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(filters.SearchValue))
        {
            var searchTerm = filters.SearchValue.Trim();

            query = query.Where(d => d.GenericName.Contains(searchTerm)
                                   || d.BrandName.Contains(searchTerm)
                                   || d.ArabicName.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filters.Form))
        {
            var formTerm = filters.Form.Trim();

            query = query.Where(d => d.Form == formTerm);
        }

        if (filters.Category.HasValue)
        {
            query = query.Where(d => d.Category == filters.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.SortColumn))
        {
            var sortCol = filters.SortColumn.Trim().ToLower() switch
            {
                "name" or "brandname" => nameof(Drug.BrandName),
                "genericname" => nameof(Drug.GenericName),
                "arabicname" => nameof(Drug.ArabicName),
                "price" => nameof(Drug.Price),
                "category" => nameof(Drug.Category),
                "date" or "createddate" or "drugid" => nameof(Drug.DrugId),
                _ => filters.SortColumn
            };

            var direction = string.Equals(filters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            query = query.OrderBy($"{sortCol} {direction}");
        }
        else
        {
            query = query.OrderBy(nameof(Drug.BrandName));
        }

        var resultQuery = query.ProjectToType<DrugDto>();

        var page = await resultQuery.ToPaginatedListAsync(filters.PageNumber, filters.PageSize, cancellationToken);

        if (filters.Latitude.HasValue && filters.Longitude.HasValue && page.Items.Count > 0)
        {
            await AttachAvailabilityAsync(page.Items, filters.Latitude.Value, filters.Longitude.Value,
                cancellationToken);
        }

        return Result.Success(page);
    }

    /// <summary>
    /// Computes each drug's AvailabilityStatus from stock at the single nearest reachable
    /// branch to the patient. If no branch is reachable at all, every drug on the page is
    /// reported OutOfStock, since nothing is realistically deliverable/pickup-able right now.
    /// </summary>
    private async Task AttachAvailabilityAsync(
        IReadOnlyCollection<DrugDto> drugs, double latitude, double longitude,
        CancellationToken cancellationToken)
    {
        var patientLocation = new Point(longitude, latitude) { SRID = 4326 };

        var nearbyBranches = await geoLookupService.FindNearbyBranchesAsync(
            patientLocation, cancellationToken: cancellationToken);

        var nearestBranch = nearbyBranches.FirstOrDefault();

        if (nearestBranch is null)
        {
            foreach (var drug in drugs)
                drug.AvailabilityStatus = DrugAvailabilityStatus.OutOfStock;

            return;
        }

        var drugIds = drugs.Select(d => d.DrugId).ToList();

        var stockByDrugId = await context.PharmacyInventories
            .AsNoTracking()
            .Where(pi => pi.BranchId == nearestBranch.BranchID && drugIds.Contains(pi.DrugId))
            .Select(pi => new { pi.DrugId, Available = pi.StockQuantity - pi.ReservedQuantity })
            .ToDictionaryAsync(pi => pi.DrugId, pi => pi.Available, cancellationToken);

        foreach (var drug in drugs)
        {
            var available = stockByDrugId.GetValueOrDefault(drug.DrugId, 0);

            drug.AvailabilityStatus = available <= 0
                ? DrugAvailabilityStatus.OutOfStock
                : available < LowStockThreshold
                    ? DrugAvailabilityStatus.LowStock
                    : DrugAvailabilityStatus.InStock;
        }
    }

    public async Task<Result<DrugDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var drug = await context.Drugs.AsNoTracking()
            .Where(d => d.IsActive && d.DrugId == id)
            .ProjectToType<DrugDto>()
            .FirstOrDefaultAsync(cancellationToken);

        return drug is null ? Result.Failure<DrugDto>(DrugErrors.DrugNotFound) : Result.Success(drug);
    }

    public async Task<Result<DrugDto>> CreateAsync(CreateDrugDto dto, CancellationToken cancellationToken = default)
    {
        var drug = dto.Adapt<Drug>();

        drug.DrugId = Guid.NewGuid();

        drug.IsActive = true;

        if (dto.Category.HasValue && dto.Category.Value != DrugCategory.Other)
        {
            drug.Category = dto.Category.Value;
        }
        else
        {
            drug.Category = DrugCategoryMapper.Map(drug.DrugClass, drug.GenericName);
        }

        context.Drugs.Add(drug);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(drug.Adapt<DrugDto>());
    }

    public async Task<Result<DrugDto>> UpdateAsync(Guid id, UpdateDrugDto dto,
        CancellationToken cancellationToken = default)
    {
        var drug = await context.Drugs.FirstOrDefaultAsync(d => d.DrugId == id && d.IsActive, cancellationToken);

        if (drug is null)
            return Result.Failure<DrugDto>(DrugErrors.DrugNotFound);

        dto.Adapt(drug);

        if (dto.Category.HasValue)
        {
            drug.Category = dto.Category.Value;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(drug.Adapt<DrugDto>());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var drug = await context.Drugs.FirstOrDefaultAsync(d => d.DrugId == id && d.IsActive, cancellationToken);

        if (drug is null)
            return Result.Failure(DrugErrors.DrugNotFound);

        drug.IsActive = false;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<List<MedicineSearchDTO>>> SearchMedicinesAsync(
        string? term,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Result.Success(new List<MedicineSearchDTO>());
        }

        var searchResults = await context.Drugs
            .Where(m => 
                EF.Functions.FreeText(m.BrandName, term) || 
                EF.Functions.FreeText(m.ArabicName, term))
            .Select(m => new MedicineSearchDTO
            {
                Id = m.DrugId,
                Name = m.BrandName,
                GenericName = m.GenericName,
                ArabicName = m.ArabicName,
                Strength = m.Strength,
                DosageForm = m.Form,
                Route = m.Form,
                Category = m.DrugClass,
                Company = m.Manufacturer,
                Price = m.Price
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        return Result.Success(searchResults);
    }

    public async Task<Result<int>> BackfillCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // The pre-migration DB default is raw 0 (byte's CLR default), which doesn't
        // correspond to any DrugCategory member (enum starts at 1). Target that raw
        // value directly instead of comparing against DrugCategory.Other.
        var drugsNeedingBackfill = await context.Drugs
            .Where(d => (byte)d.Category == 0)
            .ToListAsync(cancellationToken);

        foreach (var drug in drugsNeedingBackfill)
        {
            drug.Category = DrugCategoryMapper.Map(drug.DrugClass, drug.GenericName);
        }

        if (drugsNeedingBackfill.Count > 0)
            await context.SaveChangesAsync(cancellationToken);

        return Result.Success(drugsNeedingBackfill.Count);
    }
}