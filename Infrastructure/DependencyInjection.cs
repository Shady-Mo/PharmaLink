using Application.Services.Cart;
using Application.Services.Order;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructureServices(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions => { sqlOptions.UseNetTopologySuite(); });

                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            if (!string.IsNullOrWhiteSpace(redisConnectionString))
            {
                services.AddStackExchangeRedisCache(options => {
                    options.Configuration = redisConnectionString;
                });
            }
            else
            {
                // Fallback for environments without Redis configured
                services.AddDistributedMemoryCache();
            }

            services.AddIdentity<AppUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddJwtServices(configuration);

            services.AddHttpContextAccessor();

            services.AddScoped<IDrugService, DrugService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInventoryService, InventoryService>();

            services.AddScoped<IAddressService, AddressService>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IGeoLookupService, GeoLookupService>();
            services.AddScoped<ILegGenerationService, LegGenerationService>();
            services.AddScoped<ILegStatusTransitionService, LegStatusTransitionService>();
            services.AddScoped<IOrderSplittingService, OrderSplittingService>();
            services.AddScoped<IOrderSplittingAlgorithm, GreedyOrderSplittingAlgorithm>();

            services.AddScoped<IPrescriptionReviewService, PrescriptionReviewService>();
            services.AddScoped<IAIExtractionService, GeminiExtractionService>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IProfileService, ProfileService>();

            services.Configure<GeminiSettings>(
                configuration.GetSection(GeminiSettings.SectionName));

            services.AddHttpClient(GeminiExtractionService.HttpClientName, client =>
            {
                var settings = configuration
                    .GetSection(GeminiSettings.SectionName)
                    .Get<GeminiSettings>() ?? new GeminiSettings();

                client.Timeout = TimeSpan.FromMinutes(settings.TimeoutSeconds);
            });


            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IProfileService, ProfileService>();


            services.AddScoped<CartCacheService>();
            services.AddScoped<ICartService, CartService>();

            var webhookSettings = configuration
                .GetSection(OtpWebhookSettings.SectionName)
                .Get<OtpWebhookSettings>() ?? new OtpWebhookSettings();

            services.Configure<OtpWebhookSettings>(
                configuration.GetSection(OtpWebhookSettings.SectionName));

            services.Configure<OrderFulfillmentSettings>(
                configuration.GetSection(OrderFulfillmentSettings.SectionName));

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