using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Infrastructure.AI.Providers;
using Infrastructure.Logging;
using Microsoft.Extensions.Options;
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
///
/// DESIGN DECISION — Provider registration:
///   All IKernelProvider implementations are registered as singletons keyed by their
///   AIProvider enum value. KernelFactory and task-routing logic can resolve any provider
///   at runtime without modifying DI registration.
/// </summary>
public static class SemanticKernelDependencyInjection
{
    public static IServiceCollection AddSemanticKernelServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 1. IKernelProvider implementations are already registered in AiDependencyInjection.cs ──

        // ── 2. Register the Singleton Kernel with Plugins ─────────────────────
        services.AddSingleton(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var aiOptions = sp.GetRequiredService<IOptions<AiOptions>>().Value;

            // Default to ChatProvider config from AI:Defaults
            var providerName = aiOptions.Defaults.ChatProvider;
            if (!Enum.TryParse<AIProvider>(providerName, true, out var aiProvider))
            {
                aiProvider = AIProvider.Gemini; // Fallback
            }

            var providers = sp.GetServices<IKernelProvider>();
            var provider = providers.FirstOrDefault(p => p.Provider == aiProvider) 
                           ?? throw new InvalidOperationException($"No IKernelProvider found for {aiProvider}");

            // Resolve base kernel from the provider
            var baseKernel = provider.GetKernel(ModelRole.Chat);
            
            // Clone the kernel to add plugins safely without mutating the shared provider cache
            var kernel = baseKernel.Clone();

            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            kernel.AddPharmacyPlugins(sp);

            loggerFactory.CreateLogger("SemanticKernelDI")
                .LogInformation(
                    "Semantic Kernel initialized with unified provider '{Provider}' from AI:Defaults:ChatProvider",
                    aiProvider);

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

        // ── 5. Order Fulfillment Optimization Engine (multi-agent) ───────────
        //   The PharmacyInventoryPlugin is also resolvable directly so the orchestrator
        //   can run the deterministic evaluation used for reconciliation/fallback.
        services.AddSingleton(sp => new PharmacyInventoryPlugin(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOsrmRoutingService>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<PharmacyInventoryPlugin>()));

        services.AddScoped<Application.Services.OrderRouting.IOrderRoutingOrchestrator,
            Infrastructure.AI.OrderRouting.OrderRoutingOrchestrator>();

        return services;
    }

    private static readonly object _pluginLock = new();

    public static Kernel AddPharmacyPlugins(this Kernel kernel, IServiceProvider sp)
    {
        lock (_pluginLock)
        {
            if (kernel.Plugins.Contains("DrugPlugin"))
                return kernel;

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var osrmRoutingService = sp.GetRequiredService<IOsrmRoutingService>();

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

            kernel.Plugins.AddFromObject(
                new PharmacyInventoryPlugin(scopeFactory, osrmRoutingService, loggerFactory.CreateLogger<PharmacyInventoryPlugin>()),
                pluginName: "PharmacyInventory");

            kernel.Plugins.AddFromObject(
                new GeoDistancePlugin(osrmRoutingService, loggerFactory.CreateLogger<GeoDistancePlugin>()),
                pluginName: "GeoDistance");

            return kernel;
        }
    }
}