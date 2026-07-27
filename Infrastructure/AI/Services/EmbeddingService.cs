using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.AI.Services;

public class EmbeddingService(IKernelFactory kernelFactory)
{
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text,
        AIProvider provider = AIProvider.GitHubModels)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var kernel = kernelFactory.GetKernel(provider, ModelRole.Embedding);
        var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618 // Type or member is obsolete

        var embeddings = await embeddingService.GenerateEmbeddingsAsync([text], kernel: kernel);
        return embeddings.FirstOrDefault();
    }
}