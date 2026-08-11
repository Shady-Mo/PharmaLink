namespace Infrastructure.Persistence.Seeders;

public class AdminSeeder(
    UserManager<AppUser> userManager,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync()
    {
        const string adminEmail = "admin@example.com";

        if (await userManager.FindByEmailAsync(adminEmail) != null)
        {
            logger.LogDebug("Admin user already exists. Skipping.");
            return;
        }

        var adminUser = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Administrator",
            PhoneNumber = "01000000000",
            PhoneNumberConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "P@ss1234");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            logger.LogInformation("Admin user created successfully.");
        }
        else
        {
            logger.LogError(
                "Failed to create admin user. Errors: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}