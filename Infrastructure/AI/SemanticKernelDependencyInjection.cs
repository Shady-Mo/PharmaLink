using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.AI;

/// <summary>
/// Dependency Injection extension methods for the Semantic Kernel drug-info layer.
///
/// Registers the Kernel-based AI services (chat assistant, drug info, interaction check)
/// alongside the existing AI infrastructure (prescription extraction).
///
/// DESIGN DECISION — Singleton Kernel, Scoped services:
///   │ Component                    │ Lifetime  │ Reason                          │
///   │──────────────────────────────│───────────│─────────────────────────────────│
///   │ Kernel                       │ Singleton │ Expensive to build; thread-safe │
///   │ IChatCompletionService       │ Singleton │ Stateless HTTP wrapper           │
///   │ DrugPlugin / InventoryPlugin │ Singleton │ Stateless; use scope factory     │
///   │ IPharmacyAssistantService    │ Scoped    │ Per-request context              │
///   │ IDrugInfoService             │ Scoped    │ Same                            │
/// </summary>
public static class SemanticKernelDependencyInjection
{
    public static IServiceCollection AddSemanticKernelServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 1. Bind and validate SemanticKernel options ───────────────────────
        services
            .AddOptions<SemanticKernelSettings>()
            .Bind(configuration.GetSection(SemanticKernelSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ── 2. Register the Singleton Kernel with Plugins ─────────────────────
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SemanticKernelSettings>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var handler = new Infrastructure.Logging.HttpLoggingHandler(loggerFactory.CreateLogger<Infrastructure.Logging.HttpLoggingHandler>())
            {
                InnerHandler = new HttpClientHandler()
            };
            var httpClient = new HttpClient(handler);

            var kernel = SemanticKernelFactory.Build(settings, loggerFactory, httpClient);

            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            kernel.Plugins.AddFromObject(
                new DrugPlugin(scopeFactory, loggerFactory.CreateLogger<DrugPlugin>()),
                pluginName: "DrugPlugin");

            kernel.Plugins.AddFromObject(
                new InventoryPlugin(scopeFactory, loggerFactory.CreateLogger<InventoryPlugin>()),
                pluginName: "InventoryPlugin");

            kernel.Plugins.AddFromObject(
                new OrderPlugin(scopeFactory, loggerFactory.CreateLogger<OrderPlugin>()),
                pluginName: "OrderPlugin");

            kernel.Plugins.AddFromObject(
                new CartOrderPlugin(scopeFactory, loggerFactory.CreateLogger<CartOrderPlugin>()),
                pluginName: "CartOrderPlugin");

            loggerFactory.CreateLogger("SemanticKernelDI")
                .LogInformation(
                    "Semantic Kernel initialized with provider '{Provider}', model '{Model}'",
                    settings.Provider, settings.ModelId);

            return kernel;
        });

        // ── 3. Register IChatCompletionService (resolved from Kernel) ─────────
        services.AddSingleton<IChatCompletionService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<IChatCompletionService>();
        });

        // ── 4. Register Scoped AI services ───────────────────────────────────
        services.AddScoped<IPharmacyAssistantService, PharmacyAssistantService>();
        services.AddScoped<IDrugInfoService, DrugInfoService>();

        return services;
    }
}