using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Execution.Providers;
using Infrastructure.AI.Execution.Resilience;
using Infrastructure.AI.Execution.Routing;
using Infrastructure.AI.Factories;
using Infrastructure.AI.Options;
using Infrastructure.AI.Providers;
using Infrastructure.AI.Services;

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

        return services;
    }
}