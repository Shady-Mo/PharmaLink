namespace Infrastructure.Data.Configurations;

public class PharmacistAssignmentConfiguration : IEntityTypeConfiguration<PharmacistAssignment>
{
    public void Configure(EntityTypeBuilder<PharmacistAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssignedAt)
            .IsRequired();

        builder.Property(a => a.EndedAt)
            .IsRequired(false);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(a => a.Pharmacist)
            .WithMany(p => p.Assignments)
            .HasForeignKey(a => a.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Pharmacy)
            .WithMany()
            .HasForeignKey(a => a.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedByPharmacyAdmin)
            .WithMany()
            .HasForeignKey(a => a.AssignedByPharmacyAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PharmacistId, a.IsActive })
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_PharmacistAssignments_PharmacistId_Active");

        builder.HasIndex(a => a.PharmacistId)
            .HasDatabaseName("IX_PharmacistAssignments_PharmacistId");

        builder.HasIndex(a => a.PharmacyId)
            .HasDatabaseName("IX_PharmacistAssignments_PharmacyId");

        builder.ToTable("PharmacistAssignments");
    }
}
