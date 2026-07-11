
namespace Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructureServices(IConfiguration configuration)
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

            services.AddHttpContextAccessor();

            services.AddHttpContextAccessor();

            services.AddScoped<IDrugService, DrugService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IAddressService, AddressService>();


            var webhookSettings = configuration
                .GetSection(OtpWebhookSettings.SectionName)
                .Get<OtpWebhookSettings>() ?? new OtpWebhookSettings();

            services.Configure<OtpWebhookSettings>(
                configuration.GetSection(OtpWebhookSettings.SectionName));

            services.AddHttpClient(WebhookOtpDispatcher.HttpClientName,
                client => { client.Timeout = TimeSpan.FromSeconds(webhookSettings.TimeoutSeconds); });

            services.AddScoped<IWebhookOtpDispatcher, WebhookOtpDispatcher>();
            services.AddScoped<IOtpService, OtpService>();


            services.AddScoped<DrugSeeder>();
            services.AddScoped<RoleSeeder>();

            return services;
        }

        private IServiceCollection AddJwtServices(IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);

            if (jwtSection is null)
                throw new InvalidOperationException(
                    "Jwt configuration section is missing. Check user secrets / app settings.");

            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
            if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
                throw new InvalidOperationException("Signing key is missing. Check user secrets / app settings.");

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
                        ClockSkew = TimeSpan.Zero,

                        // Critical: map ASP.NET's [Authorize(Roles = "...")] to your RoleName claim
                        RoleClaimType = JwtClaimTypes.RoleName,
                        NameClaimType = JwtClaimTypes.UserId
                    };
                });

            services.AddSingleton<IJwtTokenGeneratorService, JwtTokenGeneratorService>();

            return services;
        }
    }
}