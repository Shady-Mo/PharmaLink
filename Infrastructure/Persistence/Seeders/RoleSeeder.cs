using System.Reflection;

namespace Infrastructure.Persistence.Seeders;

public class RoleSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<RoleSeeder> logger)
{
    public async Task SeedAsync()
    {
        var roles = typeof(AppRoles)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false })
            .Select(field => field.GetValue(null)?.ToString())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToArray();

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName!))
            {
                logger.LogDebug(
                    "Role '{Role}' already exists. Skipping.",
                    roleName);

                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName!.ToUpperInvariant()
                });

            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Role '{Role}' created successfully.",
                    roleName);
            }
            else
            {
                logger.LogError(
                    "Failed to create role '{Role}'. Errors: {Errors}",
                    roleName,
                    string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description)));
            }
        }
    }
}