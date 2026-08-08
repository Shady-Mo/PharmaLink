namespace Domain.Abstractions.RAG;

public interface IRagVectorStore<TDocument, TMetadataFilter> where TDocument : class
{
    Task IndexAsync(TDocument document, CancellationToken cancellationToken = default);

    Task IndexBatchAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult<TDocument>>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        TMetadataFilter metadataFilter,
        int topK = 20,
        double minSimilarity = 0.3,
        CancellationToken cancellationToken = default);
}

public record VectorSearchResult<TDocument>(
    TDocument Document,
    double SimilarityScore
);
