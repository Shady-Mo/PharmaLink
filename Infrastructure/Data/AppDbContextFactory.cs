namespace Infrastructure.Data;

/// <summary>
/// Design-time factory used exclusively by EF Core CLI tooling (dotnet ef migrations add / update).
/// Bypasses the full application startup host so JWT signing-key guards, Twilio settings,
/// and other run-time guards do NOT block migration scaffolding.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // The CLI runs from the Infrastructure project directory.
        // Walk one level up to locate the API project's appsettings.json.
        var apiProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "API");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(apiProjectPath))
            .AddJsonFile("appsettings.json",             optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in API/appsettings.json.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString,
            sql => sql.UseNetTopologySuite());

        return new AppDbContext(optionsBuilder.Options);
    }
}
