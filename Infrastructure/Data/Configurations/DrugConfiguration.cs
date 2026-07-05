namespace Infrastructure.Data.Configurations;

public class DrugConfiguration : IEntityTypeConfiguration<Drug> {
    public void Configure(EntityTypeBuilder<Drug> builder) {
        builder.HasKey(d => d.DrugID);
        builder.Property(d => d.GenericName).HasMaxLength(256).IsRequired();
        builder.Property(d => d.BrandName).HasMaxLength(256);
        builder.Property(d => d.DrugBankID).HasMaxLength(50);
        builder.Property(d => d.RxNormCUI).HasMaxLength(50);
        builder.Property(d => d.NdcCode).HasMaxLength(50);
        builder.Property(d => d.Strength).HasMaxLength(100);
        builder.Property(d => d.Form).HasMaxLength(100);

        builder.HasMany(d => d.Inventories)
               .WithOne(i => i.Drug)
               .HasForeignKey(i => i.DrugID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.OrderItems)
               .WithOne(oi => oi.Drug)
               .HasForeignKey(oi => oi.DrugID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
