namespace Application.Services;

public interface IDrugService
{
    Task SeedDrugsAsync(string jsonFilePath, CancellationToken cancellationToken = default);
}