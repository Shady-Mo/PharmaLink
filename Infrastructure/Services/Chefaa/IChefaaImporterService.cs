namespace Infrastructure.Services.Chefaa;

public interface IChefaaImporterService
{
    Task StartImportAsync(CancellationToken cancellationToken = default);
    object GetStatus();
}
