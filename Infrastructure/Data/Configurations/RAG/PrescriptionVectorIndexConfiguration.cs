using Domain.Entities.RAG;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.RAG;

public class PrescriptionVectorIndexConfiguration : IEntityTypeConfiguration<PrescriptionVectorIndex>
{
    public void Configure(EntityTypeBuilder<PrescriptionVectorIndex> builder)
    {
        builder.ToTable("PrescriptionVectorIndices");

        builder.HasKey(x => x.PrescriptionVectorIndexId);

        builder.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Governorate)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IndexedText)
            .IsRequired();

        builder.Property(x => x.EmbeddingJson)
            .IsRequired();

        builder.Property(x => x.MedicinesJson)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(x => new { x.City, x.CreatedAt });
        builder.HasIndex(x => new { x.BranchId, x.CreatedAt });
        builder.HasIndex(x => x.PrescriptionReviewId).IsUnique();

        // Foreign Key Relationships
        builder.HasOne(x => x.PrescriptionReview)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PharmacyBranch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
