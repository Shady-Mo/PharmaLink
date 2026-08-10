using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Execution.Providers;
using Infrastructure.AI.Execution.Resilience;
using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Factories;
using Infrastructure.AI.Options;
using Infrastructure.AI.Providers;
using Infrastructure.AI.Services;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Runtime.InteropServices;

namespace Infrastructure.AI.Extensions;

public static class AiDependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Core Orchestration Engine
        services.AddSingleton<IAIProviderRegistry, AIProviderRegistry>();
        services.AddSingleton<IAIResiliencePipelineProvider, AIResiliencePipelineProvider>();

        services.AddScoped<IRoutingStrategy, PriorityRoutingStrategy>();
        services.AddScoped<IProviderRouter, ProviderRouter>();

        // Execution Providers
        services.AddSingleton<IAIExecutionProvider, SemanticKernelExecutionProvider>();
        services.AddSingleton<IAIExecutionProvider, GeminiExecutionProvider>();
        services.AddSingleton<IAIExecutionProvider, ITIExecutionProvider>();

        // Legacy Kernel Providers for SK Caching
        services.AddSingleton<IKernelProvider, GroqProvider>();
        services.AddSingleton<IKernelProvider, GeminiProvider>();
        services.AddSingleton<IKernelProvider, GitHubModelsProvider>();
        services.AddSingleton<IKernelProvider, OpenRouterProvider>();
        services.AddSingleton<IKernelProvider, TokenRouterProvider>();
        services.AddSingleton<IKernelProvider, ITIOrderSplittingProvider>();

        // Factory
        services.AddSingleton<IKernelFactory, KernelFactory>();

        // Generic Services
        services.AddScoped<PromptExecutionService>();
        services.AddScoped<EmbeddingService>();

        // RAG Framework Services
        services.AddScoped<Domain.Abstractions.RAG.IRagVectorStore<Domain.Entities.RAG.PrescriptionVectorIndex, Application.DTOs.AI.RAG.PrescriptionMetadataFilter>,
            Infrastructure.AI.RAG.PrescriptionVectorStore>();
        services.AddScoped<Application.Services.AI.RAG.IPrescriptionAnalyticsRagService,
            Infrastructure.AI.RAG.PrescriptionAnalyticsRagService>();

        services.AddHttpClient<TranscriptionService>();
        services.AddScoped<AiHealthService>();

        
        // for qdrant client
        
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));
        
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            var tempLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("QdrantClientSetup");
            tempLogger.LogWarning(
                "Qdrant connecting to Host={Host} Port={Port} UseTls={UseTls} ApiKeySet={ApiKeySet}",
                options.Host, options.Port, options.UseTls, !string.IsNullOrWhiteSpace(options.ApiKey));

            return new QdrantClient(
                host: options.Host,
                port: options.Port,
                https: options.UseTls,
                apiKey: string.IsNullOrWhiteSpace(options.ApiKey) ? null : options.ApiKey);
        });

        return services;
    }
}