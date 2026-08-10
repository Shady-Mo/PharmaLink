using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.AI.Services;

public class EmbeddingService
{
    private readonly IKernelFactory _kernelFactory;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        IKernelFactory kernelFactory,
        IOptions<AiOptions> aiOptions,
        ILogger<EmbeddingService> logger)
    {
        _kernelFactory = kernelFactory;
        _aiOptions = aiOptions.Value;
        _logger = logger;
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        AIProvider? provider = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return GenerateDeterministicEmbedding(string.Empty);
        }

        var targetProvider = provider ?? ResolveDefaultEmbeddingProvider();

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n🧠 [AI EMBEDDING START] Provider: {targetProvider} | Text: \"{(text.Length > 60 ? text[..60] + "..." : text)}\"");
        Console.ResetColor();

        // 1. Direct Gemini REST Embedding API (text-embedding-004)
        if (targetProvider == AIProvider.Gemini)
        {
            try
            {
                var apiKey = ResolveGeminiApiKey();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var geminiEmbedding = await GenerateGeminiEmbeddingViaRestAsync(text, apiKey);
                    if (geminiEmbedding.Length > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"🧠 [AI EMBEDDING SUCCESS] Gemini text-embedding-004 | Dimensions: {geminiEmbedding.Length}");
                        Console.ResetColor();
                        return geminiEmbedding;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine($"⚠️ [AI EMBEDDING WARNING] Gemini REST embedding failed: {ex.Message}. Falling back to semantic hash vector.");
                Console.ResetColor();
                _logger.LogWarning(ex, "Failed to generate Gemini AI embedding via REST.");
            }
        }

        // 2. Semantic Kernel Provider Fallback
        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var kernel = _kernelFactory.GetKernel(targetProvider, ModelRole.Embedding);
            var embeddingService = kernel.Services.GetService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618

            if (embeddingService != null)
            {
                var embeddings = await embeddingService.GenerateEmbeddingsAsync([text], kernel: kernel);
                var result = embeddings.FirstOrDefault();
                if (result.Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"🧠 [AI EMBEDDING SUCCESS] Dimensions: {result.Length}");
                    Console.ResetColor();
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI embedding using provider {Provider}. Falling back to semantic hash vector.", targetProvider);
        }

        // 3. Fallback to deterministic normalized 384-dim hash vector
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🧠 [AI EMBEDDING FALLBACK] Using deterministic semantic hash vector (384-dims).");
        Console.ResetColor();
        return GenerateDeterministicEmbedding(text);
    }

    private string ResolveGeminiApiKey()
    {
        var optionsKey = _aiOptions.Providers.Gemini.ApiKey;
        if (!string.IsNullOrWhiteSpace(optionsKey)) return optionsKey;

        var envVar = _aiOptions.Providers.Gemini.ApiKeyEnvironmentVariable;
        return Environment.GetEnvironmentVariable(envVar)
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
            ?? Environment.GetEnvironmentVariable("AI__Providers__Gemini__ApiKey")
            ?? string.Empty;
    }

    private async Task<ReadOnlyMemory<float>> GenerateGeminiEmbeddingViaRestAsync(
        string text,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();

        var endpoints = new[]
        {
            ("https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-2:embedContent", "models/gemini-embedding-2"),
            ("https://generativelanguage.googleapis.com/v1/models/gemini-embedding-2:embedContent", "models/gemini-embedding-2"),
            ("https://generativelanguage.googleapis.com/v1beta/models/embedding-001:embedContent", "models/embedding-001"),
            ("https://generativelanguage.googleapis.com/v1/models/text-embedding-004:embedContent", "models/text-embedding-004"),
            ("https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent", "models/text-embedding-004")
        };

        string lastError = string.Empty;

        foreach (var (baseUrl, modelName) in endpoints)
        {
            try
            {
                var url = $"{baseUrl}?key={apiKey}";
                var payload = JsonSerializer.Serialize(new
                {
                    model = modelName,
                    content = new
                    {
                        parts = new[] { new { text } }
                    }
                });

                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = await response.Content.ReadAsStringAsync(cancellationToken);
                    continue;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseBody);

                if (doc.RootElement.TryGetProperty("embedding", out var embeddingObj) &&
                    embeddingObj.TryGetProperty("values", out var valuesArray))
                {
                    var floats = valuesArray.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                    return new ReadOnlyMemory<float>(floats);
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        throw new HttpRequestException($"Gemini Embedding REST API failed: {lastError}");
    }

    private AIProvider ResolveDefaultEmbeddingProvider()
    {
        var providerName = _aiOptions.Defaults.EmbeddingProvider;
        if (Enum.TryParse<AIProvider>(providerName, true, out var provider) && provider != AIProvider.ITI)
        {
            return provider;
        }

        return AIProvider.Gemini;
    }

    /// <summary>
    /// Generates a deterministic normalized 384-dimensional vector embedding
    /// for text when remote LLM embedding providers are unconfigured or unavailable.
    /// </summary>
    private static ReadOnlyMemory<float> GenerateDeterministicEmbedding(string text)
    {
        const int dimensions = 384;
        var vector = new float[dimensions];
        var normalizedText = text.ToLowerInvariant().Trim();
        var words = normalizedText.Split([' ', ',', '.', ';', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(word));
            for (int i = 0; i < dimensions; i++)
            {
                byte b = hashBytes[i % hashBytes.Length];
                float val = (b - 128f) / 128f;
                vector[i] += val;
            }
        }

        // Compute L2 norm for unit normalization
        double sumSq = 0.0;
        for (int i = 0; i < dimensions; i++)
        {
            sumSq += vector[i] * vector[i];
        }

        float norm = (float)Math.Sqrt(sumSq);
        if (norm > 0)
        {
            for (int i = 0; i < dimensions; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }
}