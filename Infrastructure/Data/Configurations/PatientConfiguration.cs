namespace Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient> {
    public void Configure(EntityTypeBuilder<Patient> builder) {
        builder.HasMany(p => p.Addresses)
               .WithOne(a => a.Patient)
               .HasForeignKey(a => a.UserID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Orders)
               .WithOne(o => o.Patient)
               .HasForeignKey(o => o.PatientUserID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
