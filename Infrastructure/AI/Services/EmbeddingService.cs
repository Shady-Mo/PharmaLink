using System.Security.Cryptography;
using System.Text;
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
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI embedding using provider {Provider}. Falling back to semantic hash vector.", targetProvider);
        }

        // Fallback to deterministic normalized embedding vector
        return GenerateDeterministicEmbedding(text);
    }

    private AIProvider ResolveDefaultEmbeddingProvider()
    {
        var providerName = _aiOptions.Defaults.EmbeddingProvider;
        if (Enum.TryParse<AIProvider>(providerName, true, out var provider))
        {
            return provider;
        }

        return AIProvider.GitHubModels;
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