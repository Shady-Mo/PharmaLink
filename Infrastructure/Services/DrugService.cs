namespace Infrastructure.Services;

public class DrugService(AppDbContext context) : IDrugService
{
    public async Task<Result<PaginatedList<DrugDto>>> SearchCatalogAsync(
        DrugSearchRequest filters,
        CancellationToken cancellationToken = default)
    {
        var query = context.Drugs
            .Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(filters.SearchValue))
        {
            var searchTerm = filters.SearchValue.Trim();

            query = query.Where(d =>
                d.BrandName.Contains(searchTerm) ||
                d.ArabicName.Contains(searchTerm) ||
                d.MetaDescriptionAr.Contains(searchTerm) ||
                d.MetaDescriptionEn.Contains(searchTerm) ||
                d.MetaKeywordsAr.Contains(searchTerm) ||
                d.MetaKeywordsEn.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filters.Form))
        {
            var form = filters.Form.Trim();

            query = query.Where(d => d.Form == form);
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == filters.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.SortColumn))
        {
            var sortDirection = string.Equals(
                filters.SortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            query = query.OrderBy($"{filters.SortColumn} {sortDirection}");
        }
        else
        {
            query = query.OrderBy(x => x.BrandName);
        }

        var source = query
            .ProjectToType<DrugDto>()
            .AsNoTracking();

        var drugs = await source.ToPaginatedListAsync(
            filters.PageNumber,
            filters.PageSize,
            cancellationToken);

        return Result.Success(drugs);
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

        if (dto.CategoryId.HasValue)
        {
            drug.CategoryId = dto.CategoryId.Value;
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

        if (dto.CategoryId.HasValue)
        {
            drug.CategoryId = dto.CategoryId.Value;
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
                EF.Functions.FreeText(m.ArabicName, term) ||
                m.BrandName.Contains(term) ||
                m.ArabicName.Contains(term) ||
                m.MetaDescriptionAr.Contains(term) ||
                m.MetaDescriptionEn.Contains(term) ||
                m.MetaKeywordsAr.Contains(term) ||
                m.MetaKeywordsEn.Contains(term))
            .Select(m => new MedicineSearchDTO
            {
                Id = m.DrugId,
                Name = m.BrandName,
                Category = m.Category != null ? m.Category.NameEn : string.Empty,
                ArabicName = m.ArabicName,
                DosageForm = m.Form,
                Route = m.Form,
                Company = m.Manufacturer,
                Price = m.Price
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        return Result.Success(searchResults);
    }
}