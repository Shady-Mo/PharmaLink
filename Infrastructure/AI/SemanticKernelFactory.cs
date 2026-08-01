using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.AI;

/// <summary>
/// Factory responsible for constructing and configuring the Semantic Kernel instance.
///
/// DESIGN DECISION — Singleton Kernel:
///   SK's Kernel is thread-safe and expensive to construct (~100ms due to reflection
///   scanning plugins and initializing HTTP client pipelines). We register it as a
///   Singleton so the cost is paid once at startup. All per-request state lives in
///   ChatHistory objects, which are created per-call inside the service methods.
///
/// DESIGN DECISION — Static factory vs. IHostedService:
///   A static factory keeps construction logic testable in isolation (just call the
///   method with a mock IServiceProvider). IHostedService would complicate testing
///   and introduce async startup complexity we do not need here.
/// </summary>
public static class SemanticKernelFactory
{
    /// <summary>
    /// Builds and returns a fully configured <see cref="Kernel"/> instance.
    /// </summary>
    /// <param name="settings">Options bound from "SemanticKernel" config section.</param>
    /// <param name="loggerFactory">Logger factory forwarded to SK's internal logger.</param>
    /// <returns>A ready-to-use Kernel.</returns>
    /// <exception cref="InvalidOperationException">
    ///   Thrown if the provider name is unsupported.
    /// </exception>
    public static Kernel Build(SemanticKernelSettings settings, ILoggerFactory loggerFactory, HttpClient? httpClient = null)
    {
        var builder = Kernel.CreateBuilder();

        // Forward the application's ILoggerFactory into SK so all SK log output
        // uses the same logging pipeline (Serilog, Application Insights, etc.).
        builder.Services.AddSingleton(loggerFactory);

        RegisterAiProvider(builder, settings, httpClient);

        return builder.Build();
    }

    // ---------------------------------------------------------------------------
    //  Private helpers
    // ---------------------------------------------------------------------------

    private static void RegisterAiProvider(IKernelBuilder builder, SemanticKernelSettings settings, HttpClient? httpClient)
    {
        switch (settings.Provider.Trim().ToLowerInvariant())
        {
            case "googlegemini":
            case "gemini":
                // DESIGN DECISION — Google Gemini (default):
                //   We reuse the same Google Gemini API key already configured for
                //   GeminiExtractionService. This avoids managing two sets of credentials.
                //   The Google connector exposes IChatCompletionService, enabling automatic
                //   function calling via FunctionChoiceBehavior.Auto().
                builder.AddGoogleAIGeminiChatCompletion(
                    modelId: settings.ModelId,
                    apiKey: settings.ApiKey,
                    httpClient: httpClient);
                break;

            case "azureopenai":
            case "azure":
                // Azure OpenAI requires both an endpoint and a deployment name.
                if (string.IsNullOrWhiteSpace(settings.Endpoint))
                    throw new InvalidOperationException(
                        "SemanticKernel:Endpoint is required when Provider is 'AzureOpenAI'.");

                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: settings.ModelId,
                    endpoint: settings.Endpoint,
                    apiKey: settings.ApiKey,
                    httpClient: httpClient);
                break;

            case "openai":
            default:
                builder.AddOpenAIChatCompletion(
                    modelId: settings.ModelId,
                    apiKey: settings.ApiKey,
                    httpClient: httpClient);
                break;
        }
    }
}
