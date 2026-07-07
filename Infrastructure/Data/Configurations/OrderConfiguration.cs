namespace Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.OrderId);

        builder
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.FulfillmentLegs)
            .WithOne(l => l.Order)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}