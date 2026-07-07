namespace Infrastructure.Data.Configurations;

public class PharmacyConfiguration : IEntityTypeConfiguration<Pharmacy>
{
    public void Configure(EntityTypeBuilder<Pharmacy> builder)
    {
        builder.HasKey(p => p.PharmacyId);

        builder
            .Property(p => p.LegalName)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(p => p.LicenseNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasMany(p => p.Branches)
            .WithOne(b => b.Pharmacy)
            .HasForeignKey(b => b.PharmacyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}