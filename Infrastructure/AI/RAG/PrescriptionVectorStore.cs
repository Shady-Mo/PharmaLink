using System.Text.Json;
using Application.DTOs.AI.RAG;
using Domain.Abstractions.RAG;
using Domain.Entities.RAG;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI.RAG;

public class PrescriptionVectorStore : IRagVectorStore<PrescriptionVectorIndex, PrescriptionMetadataFilter>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PrescriptionVectorStore> _logger;

    public PrescriptionVectorStore(
        AppDbContext dbContext,
        ILogger<PrescriptionVectorStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task IndexAsync(PrescriptionVectorIndex document, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.PrescriptionVectorIndices
            .FirstOrDefaultAsync(x => x.PrescriptionReviewId == document.PrescriptionReviewId, cancellationToken);

        if (existing != null)
        {
            existing.BranchId = document.BranchId;
            existing.City = document.City;
            existing.Governorate = document.Governorate;
            existing.IndexedText = document.IndexedText;
            existing.EmbeddingJson = document.EmbeddingJson;
            existing.MedicinesJson = document.MedicinesJson;
            existing.IsPediatric = document.IsPediatric;
            existing.UpdatedAt = DateTime.UtcNow;
            _dbContext.PrescriptionVectorIndices.Update(existing);
        }
        else
        {
            await _dbContext.PrescriptionVectorIndices.AddAsync(document, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task IndexBatchAsync(IEnumerable<PrescriptionVectorIndex> documents, CancellationToken cancellationToken = default)
    {
        foreach (var doc in documents)
        {
            var existing = await _dbContext.PrescriptionVectorIndices
                .FirstOrDefaultAsync(x => x.PrescriptionReviewId == doc.PrescriptionReviewId, cancellationToken);

            if (existing != null)
            {
                existing.BranchId = doc.BranchId;
                existing.City = doc.City;
                existing.Governorate = doc.Governorate;
                existing.IndexedText = doc.IndexedText;
                existing.EmbeddingJson = doc.EmbeddingJson;
                existing.MedicinesJson = doc.MedicinesJson;
                existing.IsPediatric = doc.IsPediatric;
                existing.UpdatedAt = DateTime.UtcNow;
                _dbContext.PrescriptionVectorIndices.Update(existing);
            }
            else
            {
                await _dbContext.PrescriptionVectorIndices.AddAsync(doc, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult<PrescriptionVectorIndex>>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        PrescriptionMetadataFilter metadataFilter,
        int topK = 20,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PrescriptionVectorIndices.AsNoTracking().AsQueryable();

        // 1. Branch restriction — only applied when user requests a specific branch's data
        if (metadataFilter.RestrictedBranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == metadataFilter.RestrictedBranchId.Value);
        }

        // 2. Geographic filters (market-wide — not branch restricted)
        if (!string.IsNullOrWhiteSpace(metadataFilter.City))
        {
            var rawCity = metadataFilter.City.Trim();
            var normCity = rawCity.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا').Replace('ة', 'ه').Replace('ى', 'ي');

            query = query.Where(x =>
                x.City.Contains(rawCity) ||
                rawCity.Contains(x.City) ||
                x.City.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ة", "ه").Replace("ى", "ي").Contains(normCity) ||
                normCity.Contains(x.City.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ة", "ه").Replace("ى", "ي")));
        }

        if (!string.IsNullOrWhiteSpace(metadataFilter.Governorate))
        {
            var rawGov = metadataFilter.Governorate.Trim();
            var normGov = rawGov.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا').Replace('ة', 'ه').Replace('ى', 'ي');

            query = query.Where(x =>
                x.Governorate.Contains(rawGov) ||
                rawGov.Contains(x.Governorate) ||
                x.Governorate.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ة", "ه").Replace("ى", "ي").Contains(normGov) ||
                normGov.Contains(x.Governorate.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ة", "ه").Replace("ى", "ي")));
        }

        // 3. Pediatric filter — only when explicitly flagged from question
        if (metadataFilter.IsPediatric.HasValue && metadataFilter.IsPediatric.Value)
        {
            query = query.Where(x => x.IsPediatric);
        }

        var candidates = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("No prescription vector candidates matched SQL metadata filters.");
            return Array.Empty<VectorSearchResult<PrescriptionVectorIndex>>();
        }


        // 2. Cosine Similarity Calculation in C#
        var results = new List<VectorSearchResult<PrescriptionVectorIndex>>();
        var querySpan = queryVector.Span;

        foreach (var candidate in candidates)
        {
            try
            {
                var floatArr = JsonSerializer.Deserialize<float[]>(candidate.EmbeddingJson);
                if (floatArr == null || floatArr.Length == 0) continue;

                var score = ComputeCosineSimilarity(querySpan, floatArr);
                if (score >= minSimilarity)
                {
                    results.Add(new VectorSearchResult<PrescriptionVectorIndex>(candidate, score));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse embedding JSON for PrescriptionVectorIndex {Id}", candidate.PrescriptionVectorIndexId);
            }
        }

        return results
            .OrderByDescending(r => r.SimilarityScore)
            .Take(topK)
            .ToList();
    }

    private static double ComputeCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length == 0 || vectorB.Length == 0) return 0.0;

        int length = Math.Min(vectorA.Length, vectorB.Length);
        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
