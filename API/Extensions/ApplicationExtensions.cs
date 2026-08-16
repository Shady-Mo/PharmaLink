namespace API.Extensions;

public static class ApplicationExtensions
{
    public static async Task<WebApplication> ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        //var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
        //await roleSeeder.SeedAsync();

        //var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
        //await adminSeeder.SeedAsync();

        //var osmSeeder = scope.ServiceProvider.GetRequiredService<OsmPharmacySeeder>();
        //await osmSeeder.SeedAsync();

        var inventorySeeder = scope.ServiceProvider.GetRequiredService<InventorySeeder>();
        await inventorySeeder.SeedAsync();

        // var seeder = scope.ServiceProvider.GetRequiredService<DrugSeeder>();
        //
        // var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        //
        // var jsonPath = Path.Combine(env.WebRootPath, "Data", "egyptian-drugs.json");
        //
        // await seeder.SeedAsync(jsonPath);

        app.MapGet("/test-seed", async (AppDbContext ctx, OsmPharmacySeeder osm, InventorySeeder inv) => 
        {
            var pCount = await ctx.Pharmacies.CountAsync();
            var bCount = await ctx.PharmacyBranches.CountAsync();
            var dCount = await ctx.Drugs.CountAsync();
            var iCount = await ctx.PharmacyInventories.CountAsync();
            
            return new {
                Before = new { Pharmacies = pCount, Branches = bCount, Drugs = dCount, Inventories = iCount },
                Message = "Run seeders by removing the AnyAsync guards or checking the logs."
            };
        });

        return app;
    }
}
