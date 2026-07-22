namespace Application.Services;

public interface IDrugService
{
    Task<Result<PaginatedList<DrugDto>>> SearchCatalogAsync(DrugSearchRequest filters,
        CancellationToken cancellationToken = default);

    Task<Result<DrugDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<DrugDto>> CreateAsync(CreateDrugDto dto, CancellationToken cancellationToken = default);

    Task<Result<DrugDto>> UpdateAsync(Guid id, UpdateDrugDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<int>> BackfillCategoriesAsync(CancellationToken cancellationToken = default);

    Task<Result<List<MedicineSearchDTO>>> SearchMedicinesAsync(string? term);
}