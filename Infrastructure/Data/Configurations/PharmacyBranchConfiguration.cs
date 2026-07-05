namespace Infrastructure.Data.Configurations;

public class PharmacyBranchConfiguration : IEntityTypeConfiguration<PharmacyBranch> {
    public void Configure(EntityTypeBuilder<PharmacyBranch> builder) {
        builder.HasKey(b => b.BranchID);
        builder.Property(b => b.BranchName).HasMaxLength(256).IsRequired();
        builder.Property(b => b.City).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Governorate).HasMaxLength(100).IsRequired();
        builder.Property(b => b.ServiceRadiusKm).HasColumnType("decimal(18,2)");

        builder.HasMany(b => b.Inventories)
               .WithOne(i => i.Branch)
               .HasForeignKey(i => i.BranchID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.SuppliedOrderItems)
               .WithOne(oi => oi.Branch)
               .HasForeignKey(oi => oi.BranchID)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.FulfillmentLegs)
               .WithOne(l => l.Branch)
               .HasForeignKey(l => l.BranchID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
