namespace Infrastructure.Services;

public class DrugService(AppDbContext context) : IDrugService
{
    public async Task<Result<PaginatedList<DrugDto>>> SearchCatalogAsync(
        DrugSearchRequest filters,
        CancellationToken cancellationToken = default)
    {
        var query = context.Drugs.AsNoTracking().Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(filters.SearchValue))
        {
            var searchTerm = filters.SearchValue.Trim();
            query = query.Where(d => d.GenericName.Contains(searchTerm) || d.BrandName.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filters.Form))
        {
            var formTerm = filters.Form.Trim();
            query = query.Where(d => d.Form == formTerm);
        }

        if (!string.IsNullOrWhiteSpace(filters.SortColumn))
        {
            var direction = string.Equals(filters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";
            query = query.OrderBy($"{filters.SortColumn} {direction}");
        }
        else
        {
            query = query.OrderBy(x => x.BrandName);
        }

        var resultQuery = query.ProjectToType<DrugDto>();

        return Result.Success(
            await resultQuery.ToPaginatedListAsync(filters.PageNumber, filters.PageSize, cancellationToken));
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
}