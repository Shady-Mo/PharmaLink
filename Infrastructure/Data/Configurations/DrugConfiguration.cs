namespace Infrastructure.Data.Configurations;

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.HasKey(d => d.DrugId);


        builder
            .Property(d => d.GenericName)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(d => d.BrandName)
            .HasMaxLength(256);

        builder
            .Property(d => d.DrugBankId)
            .HasMaxLength(50);

        builder
            .Property(d => d.RxNormCui)
            .HasMaxLength(50);


        builder
            .Property(d => d.NdcCode)
            .HasMaxLength(50);


        builder
            .Property(d => d.Strength)
            .HasMaxLength(100);


        builder
            .Property(d => d.Form)
            .HasMaxLength(100);

        builder.HasMany(d => d.Inventories)
            .WithOne(i => i.Drug)
            .HasForeignKey(i => i.DrugId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.OrderItems)
            .WithOne(oi => oi.Drug)
            .HasForeignKey(oi => oi.DrugId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}