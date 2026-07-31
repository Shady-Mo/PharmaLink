using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class PrescriptionReviewConfiguration : IEntityTypeConfiguration<PrescriptionReview>
{
    public void Configure(EntityTypeBuilder<PrescriptionReview> builder)
    {
        builder.HasKey(r => r.PrescriptionReviewId);

        builder.Property(r => r.PrescriptionImagePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.AIModel)
            .HasMaxLength(100);

        builder.Property(r => r.ExtractedText);

        builder.Property(r => r.AISummary)
            .HasMaxLength(2000);

        builder.Property(r => r.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(PrescriptionProcessingStatus.PendingPharmacistReview)
            .IsRequired();

        builder.Property(r => r.ReviewNotes)
            .HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasOne(r => r.Patient)
            .WithMany(p => p.PrescriptionReviews)
            .HasForeignKey(r => r.PatientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Pharmacist)
            .WithMany(ph => ph.ReviewedPrescriptions)
            .HasForeignKey(r => r.PharmacistUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.CreatedOrder)
            .WithOne(o => o.PrescriptionReview)
            .HasForeignKey<PrescriptionReview>(r => r.CreatedOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Medicines)
            .WithOne(m => m.PrescriptionReview)
            .HasForeignKey(m => m.PrescriptionReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
