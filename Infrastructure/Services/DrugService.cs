namespace Infrastructure.Services;

public class DrugService(AppDbContext context, ILogger<DrugService> logger) : IDrugService
{
    public async Task SeedDrugsAsync(
        string jsonFilePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(jsonFilePath))
        {
            logger.LogError("Seed file not found at path: {Path}", jsonFilePath);
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

        DrugSeedRoot? data;

        try
        {
            data = JsonSerializer.Deserialize<DrugSeedRoot>(jsonContent);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize the drug seed file.");
            return;
        }

        if (data?.Data is null || !data.Data.Any())
        {
            logger.LogWarning("No data found in the JSON seed file.");
            return;
        }

        var ndcCodes = new HashSet<string>(
            await context.Drugs
                .AsNoTracking()
                .Select(d => d.NdcCode)
                .ToListAsync(cancellationToken));

        var drugsToAdd = new List<Drug>();

        foreach (var item in data.Data)
        {
            if (string.IsNullOrWhiteSpace(item.Barcode) || ndcCodes.Contains(item.Barcode))
            {
                logger.LogWarning(
                    "Duplicate detected. Drug with NdcCode '{NdcCode}' already exists.",
                    item.Barcode);

                continue;
            }

            drugsToAdd.Add(new Drug
            {
                DrugId = Guid.NewGuid(),
                BrandName = item.Name,
                GenericName = item.ActiveIngredient,
                Form = item.DosageForm,
                NdcCode = item.Barcode,
                IsActive = true,
                DrugBankId = "NF",
                RxNormCui = "NF",
                Strength = "NF",
                RequiresPrescription = false
            });

            ndcCodes.Add(item.Barcode);
        }

        if (!drugsToAdd.Any())
        {
            logger.LogInformation("Catalog is already up to date. No new drugs were seeded.");
            return;
        }

        await context.Drugs.AddRangeAsync(drugsToAdd, cancellationToken);

        var insertedCount = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} drug(s) were successfully seeded into the catalog.",
            insertedCount);
    }

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