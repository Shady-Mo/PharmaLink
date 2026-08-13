using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RecurringPrescriptionConfiguration : IEntityTypeConfiguration<RecurringPrescription>
{
    public void Configure(EntityTypeBuilder<RecurringPrescription> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId);
        builder.HasOne(r => r.PreferredBranch).WithMany().HasForeignKey(r => r.PreferredBranchId).IsRequired(false);
        builder.HasOne(r => r.Prescription).WithMany().HasForeignKey(r => r.PrescriptionId).IsRequired(false);
        builder.HasMany(r => r.Runs).WithOne(run => run.RecurringPrescription).HasForeignKey(run => run.RecurringPrescriptionId);
    }
}
