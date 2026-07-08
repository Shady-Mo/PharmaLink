namespace API.Extensions;

public static class ApplicationExtensions
{
    public static async Task<WebApplication> ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();

        await roleSeeder.SeedAsync();

        // var seeder = scope.ServiceProvider.GetRequiredService<DrugSeeder>();
        //
        // var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        //
        // var jsonPath = Path.Combine(env.WebRootPath, "Data", "egyptian-drugs.json");
        //
        // await seeder.SeedAsync(jsonPath);

        return app;
    }
}