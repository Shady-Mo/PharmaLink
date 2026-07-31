using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class MedicalInquiryConfiguration : IEntityTypeConfiguration<MedicalInquiry>
{
    public void Configure(EntityTypeBuilder<MedicalInquiry> builder)
    {
        builder.HasKey(i => i.MedicalInquiryId);

        builder.Property(i => i.Question)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Answer)
            .HasMaxLength(4000);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(i => i.Patient)
            .WithMany()
            .HasForeignKey(i => i.PatientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AnsweredBy)
            .WithMany()
            .HasForeignKey(i => i.AnsweredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.Status, i.CreatedAt });
        builder.HasIndex(i => i.PatientUserId);
    }
}
