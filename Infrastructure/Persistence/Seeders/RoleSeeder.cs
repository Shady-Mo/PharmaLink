namespace Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the three application roles into the ASP.NET Core Identity role store.
/// This seeder is fully idempotent — it is safe to run on every application startup.
/// </summary>
public class RoleSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<RoleSeeder> logger)
{
    private static readonly string[] Roles =
    [
        AppRoles.Patient,
        AppRoles.Pharmacist,
        AppRoles.Admin
    ];

    public async Task SeedAsync()
    {
        foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogDebug("Role '{Role}' already exists. Skipping.", roleName);
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            if (result.Succeeded)
                logger.LogInformation("Role '{Role}' created successfully.", roleName);
            else
                logger.LogError(
                    "Failed to create role '{Role}'. Errors: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}