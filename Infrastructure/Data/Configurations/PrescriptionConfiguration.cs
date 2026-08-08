using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(p => p.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Order)
            .WithOne(o => o.Prescription)
            .HasForeignKey<Prescription>(p => p.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
