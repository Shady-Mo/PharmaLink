using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Factories;
using Infrastructure.AI.Options;
using Infrastructure.AI.Providers;
using Infrastructure.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.AI.Extensions;

public static class AiDependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Register Providers as Singletons for Kernel Caching
        services.AddSingleton<IKernelProvider, GroqProvider>();
        services.AddSingleton<IKernelProvider, GeminiProvider>();
        services.AddSingleton<IKernelProvider, GitHubModelsProvider>();

        // Register Factory as Singleton
        services.AddSingleton<IKernelFactory, KernelFactory>();

        // Register Generic Services
        services.AddScoped<PromptExecutionService>();
        services.AddScoped<EmbeddingService>();
        
        services.AddHttpClient<TranscriptionService>();

        services.AddScoped<AiHealthService>();

        return services;
    }
}
