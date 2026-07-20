namespace Application.Services;

public interface IDrugService
{
    Task<Result<PaginatedList<DrugDto>>> SearchCatalogAsync(DrugSearchRequest filters,
        CancellationToken cancellationToken = default);

    Task<Result<DrugDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<DrugDto>> CreateAsync(CreateDrugDto dto, CancellationToken cancellationToken = default);
    Task<Result<DrugDto>> UpdateAsync(Guid id, UpdateDrugDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// One-time maintenance operation: assigns Category to any already-seeded drug that
    /// predates the Category column (Category == default/Other with an unmapped DrugClass).
    /// New seeds/creates get Category automatically — this is only for backfilling old rows.
    /// </summary>
    Task<Result<int>> BackfillCategoriesAsync(CancellationToken cancellationToken = default);

}