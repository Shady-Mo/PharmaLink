using Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");


        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions => { sqlOptions.UseNetTopologySuite(); });
        });

        services.AddIdentity<AppUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddJwtServices(configuration);


        services.AddScoped<IDrugService, DrugService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInventoryService, InventoryService>();
        
        services.AddScoped<DrugSeeder>();
        services.AddScoped<RoleSeeder>();

        return services;
    }
    private static IServiceCollection AddJwtServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        if (jwtSection is null)
            throw new InvalidOperationException("Jwt configuration section is missing. Check user secrets / appsettings.");
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
            throw new InvalidOperationException("Signing key is missing. Check user secrets / appsettings.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));


        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

                // Critical: map ASP.NET's [Authorize(Roles = "...")] to your RoleName claim
                RoleClaimType = JwtClaimTypes.RoleName,
                NameClaimType = JwtClaimTypes.UserId
            };
        });

        services.AddSingleton<IJwtTokenGeneratorService, JwtTokenGeneratorService>();

        return services;
    }
}