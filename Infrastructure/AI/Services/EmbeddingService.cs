using Infrastructure.AI.Factories;
using Infrastructure.AI.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.AI.Services;

public class EmbeddingService
{
    private readonly IKernelFactory _kernelFactory;

    public EmbeddingService(IKernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, AIProvider provider = AIProvider.GitHubModels)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var kernel = _kernelFactory.GetKernel(provider, ModelRole.Embedding);
        var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618 // Type or member is obsolete
        
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(new[] { text }, kernel: kernel);
        return embeddings.FirstOrDefault();
    }
}
