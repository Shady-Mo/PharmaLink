namespace Infrastructure.Data.Configurations;

public class PharmacistConfiguration : IEntityTypeConfiguration<Pharmacist> {
    public void Configure(EntityTypeBuilder<Pharmacist> builder) {
        builder.HasMany(p => p.AdministeredPharmacies)
               .WithOne(ph => ph.Owner)
               .HasForeignKey(ph => ph.OwnerUserID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
