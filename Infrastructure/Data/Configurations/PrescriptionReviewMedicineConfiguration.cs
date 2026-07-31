using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class PrescriptionReviewMedicineConfiguration : IEntityTypeConfiguration<PrescriptionReviewMedicine>
{
    public void Configure(EntityTypeBuilder<PrescriptionReviewMedicine> builder)
    {
        builder.HasKey(m => m.PrescriptionReviewMedicineId);

        builder.Property(m => m.MedicineName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.OriginalMedicineName)
            .HasMaxLength(500);

        builder.Property(m => m.GenericName).HasMaxLength(500);
        builder.Property(m => m.Strength).HasMaxLength(100);
        builder.Property(m => m.DosageForm).HasMaxLength(100);
        builder.Property(m => m.Dose).HasMaxLength(200);
        builder.Property(m => m.Frequency).HasMaxLength(200);
        builder.Property(m => m.Duration).HasMaxLength(200);
        builder.Property(m => m.Route).HasMaxLength(100);

        builder.Property(m => m.MatchStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(PrescriptionMedicineMatchStatus.NotFound)
            .IsRequired();

        builder.Property(m => m.MatchReason)
            .HasMaxLength(1000);

        builder.Property(m => m.RequiresPatientApproval)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.IsEdited)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.Quantity)
            .HasDefaultValue(1)
            .IsRequired();

        builder.HasOne(m => m.MatchedDrug)
            .WithMany()
            .HasForeignKey(m => m.MatchedDrugId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.SuggestedAlternativeDrug)
            .WithMany()
            .HasForeignKey(m => m.SuggestedAlternativeDrugId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
