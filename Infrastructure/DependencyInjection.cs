using Hangfire;
using Infrastructure.BackgroundJobs;

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

            if (string.IsNullOrWhiteSpace(redisConnectionString))
                throw new InvalidOperationException("Connection string 'Redis' was not found.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions => { sqlOptions.UseNetTopologySuite(); });

                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            services.AddStackExchangeRedisCache(options => { options.Configuration = redisConnectionString; });

            services.AddIdentity<AppUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddJwtServices(configuration);

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddHttpClient<IWebhookOtpDispatcher, WhapiWhatsAppOtpDispatcher>();

            services.AddScoped<IDrugService, DrugService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInventoryService, InventoryService>();

            services.AddScoped<IAddressService, AddressService>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPharmacyOrderService, PharmacyOrderService>();

            services.AddScoped<IGeoLookupService, GeoLookupService>();
            services.AddScoped<ILegGenerationService, LegGenerationService>();
            services.AddScoped<ILegStatusTransitionService, LegStatusTransitionService>();
            services.AddScoped<IOrderSplittingService, OrderSplittingService>();
            services.AddScoped<IOrderSplittingAlgorithm, GreedyOrderSplittingAlgorithm>();

            services.AddScoped<IPrescriptionReviewService, PrescriptionReviewService>();
            services.AddScoped<IMedicalInquiryService, MedicalInquiryService>();
            services.AddSingleton<IAgentProfileProvider, StaticAgentProfileProvider>();
            services.AddScoped<IPromptRegistry, FileSystemPromptRegistry>();

            services.AddScoped<IPromptExecutionService, SemanticKernelPromptExecutionService>();
            services.AddScoped<IPrescriptionExtractionService, PrescriptionExtractionService>();
            services.AddScoped<IMedicineImageExtractionService, MedicineImageExtractionService>();
            services.AddScoped<IAIResponseValidator<AIExtractionResult>, PrescriptionExtractionBusinessValidator>();
            services
                .AddScoped<IAIResponseValidator<MedicineImageExtractionResponseDTO>,
                    MedicineImageExtractionBusinessValidator>();
            services.AddScoped<IDrugCatalogPlugin, DrugCatalogPlugin>();
            services.AddScoped<IAlternativeSearchPlugin, AlternativeSearchPlugin>();
            services.AddScoped<ICartBuilderPlugin, CartBuilderPlugin>();
            services.AddScoped<IPrescriptionAuditAgent, PrescriptionAuditAgent>();
            services.AddSingleton<IPrescriptionAuditJobQueue, PrescriptionAuditJobQueue>();
            services.AddHostedService<PrescriptionAuditBackgroundService>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPharmacistProfileService, PharmacistProfileService>();

            services.AddScoped<IPharmacyBranchService, PharmacyBranchService>();
            services.AddScoped<IPharmacyService, PharmacyService>();
            services.AddScoped<IPharmacyProfileService, PharmacyProfileService>();

            services.AddScoped<IAdminPharmacyService, AdminPharmacyService>();
            services.AddScoped<IPharmacyOwnerService, PharmacyOwnerService>();

            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IAdminUserService, AdminUserService>();

            services.AddScoped<IPharmacistManagementService, PharmacistManagementService>();

            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IPatientPharmacyService, PatientPharmacyService>();
            services.AddScoped<IPharmacyBranchScheduleService, PharmacyBranchScheduleService>();

            services.AddScoped<IDashboardService, DashboardService>();

            services.AddScoped<IPharmacyDashboardService, PharmacyDashboardService>();
            services.AddScoped<IPreparationListService, PreparationListService>();
            services.AddScoped<IOrderFulfillmentLegService, OrderFulfillmentLegService>();

            services.AddScoped<IPharmacistDashboardService, PharmacistDashboardService>();

            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<IPharmacyAdminService, PharmacyAdminService>();

            services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
            services.AddScoped<IInventoryReportService, InventoryReportService>();
            services.AddScoped<ISupplierOrderService, SupplierOrderService>();

            services.AddScoped<ISupplierDrugService, SupplierDrugService>();
            services.AddScoped<ISupplierProfileService, SupplierProfileService>();

            services.AddScoped<ISupplierOrderService, SupplierOrderService>();
            services.Configure<GeminiSettings>(
                configuration.GetSection(GeminiSettings.SectionName));

            services.AddTransient<GeminiRetryHandler>();

            services.AddHttpClient(GeminiExtractionService.HttpClientName, client =>
                {
                    var settings = configuration
                        .GetSection(GeminiSettings.SectionName)
                        .Get<GeminiSettings>() ?? new GeminiSettings();

                    client.Timeout = TimeSpan.FromMinutes(settings.TimeoutSeconds);
                })
                .AddHttpMessageHandler<GeminiRetryHandler>();


            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPharmacistProfileService, PharmacistProfileService>();


            services.AddScoped<CartCacheService>();
            services.AddScoped<ICartService, CartService>();

            var webhookSettings = configuration
                .GetSection(OtpWebhookSettings.SectionName)
                .Get<OtpWebhookSettings>() ?? new OtpWebhookSettings();

            services.Configure<OtpWebhookSettings>(
                configuration.GetSection(OtpWebhookSettings.SectionName));

            services.Configure<OrderFulfillmentSettings>(
                configuration.GetSection(OrderFulfillmentSettings.SectionName));
            //
            // services.AddHttpClient(WebhookOtpDispatcher.HttpClientName,
            //     client => { client.Timeout = TimeSpan.FromSeconds(webhookSettings.TimeoutSeconds); });

            //services.AddScoped<IWebhookOtpDispatcher, WebhookOtpDispatcher>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IInventoryForecastingService, InventoryForecastingService>();

            // OSRM Routing Service — sole source of truth for driving distance/duration calculation
            services.AddHttpClient("OsrmClient", client =>
            {
                client.BaseAddress = new Uri("https://router.project-osrm.org/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            // Singleton: OsrmRoutingService is stateless (only depends on IHttpClientFactory + ILogger),
            // and it is consumed by the singleton PharmacyInventoryPlugin, so it must not be scoped.
            services.AddSingleton<IOsrmRoutingService, OsrmRoutingService>();

            services.AddScoped<DrugSeeder>();
            services.AddScoped<RoleSeeder>();


            // Program.cs / DependencyInjection.cs
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<PrescriptionEmbeddingHostedService>();
            services.AddScoped<IPatientPrescriptionVectorService, QdrantPatientPrescriptionVectorService>();
            services.AddScoped<PatientPrescriptionSearchPlugin>();


            services.AddHttpClient<Infrastructure.Services.Chefaa.IChefaaApiClient, Infrastructure.Services.Chefaa.ChefaaApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://meilisearch.chefaa.com/");
            });
            services.AddScoped<Infrastructure.Services.Chefaa.IChefaaImporterService, Infrastructure.Services.Chefaa.ChefaaImporterService>();

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));

            services.AddHangfireServer();

            services.AddAiInfrastructure(configuration);
            services.AddSemanticKernelServices(configuration);

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

                        RoleClaimType = JwtClaimTypes.RoleName,
                        NameClaimType = JwtClaimTypes.UserId
                    };
                });

            services.AddSingleton<IJwtTokenGeneratorService, JwtTokenGeneratorService>();

            return services;
        }
    }
}