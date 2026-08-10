using Domain.Entities;

namespace Domain.Entities.RAG;

public class PrescriptionVectorIndex
{
    public Guid PrescriptionVectorIndexId { get; set; } = Guid.NewGuid();

    public Guid PrescriptionReviewId { get; set; }

    public Guid? BranchId { get; set; }

    public string City { get; set; } = string.Empty;

    public string Governorate { get; set; } = string.Empty;

    public string IndexedText { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized float[] embedding array.
    /// </summary>
    public string EmbeddingJson { get; set; } = "[]";

    /// <summary>
    /// JSON serialized list of prescription medicines metadata.
    /// </summary>
    public string MedicinesJson { get; set; } = "[]";

    public bool IsPediatric { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PrescriptionReview PrescriptionReview { get; set; } = null!;
    public PharmacyBranch? PharmacyBranch { get; set; }
}
