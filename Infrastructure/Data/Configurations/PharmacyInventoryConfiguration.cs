namespace Infrastructure.Data.Configurations;

public class PharmacyInventoryConfiguration : IEntityTypeConfiguration<PharmacyInventory>
{
    public void Configure(EntityTypeBuilder<PharmacyInventory> builder)
    {
        builder.HasKey(i => i.InventoryId);
        
        builder
            .Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");
        
        builder
            .Property(i => i.RowVersion)
            .IsRowVersion();
    }
}