namespace Infrastructure.Data.Configurations;

public class DrugSupplierConfiguration : IEntityTypeConfiguration<DrugSupplier>
{
    public void Configure(EntityTypeBuilder<DrugSupplier> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Discount).HasPrecision(18, 2);
        builder.Property(s => s.CommercialPrice).HasPrecision(18, 2);
        builder.Property(s => s.Price).HasPrecision(18, 2);
    }
}
