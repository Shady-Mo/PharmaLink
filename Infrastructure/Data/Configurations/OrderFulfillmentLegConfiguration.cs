namespace Infrastructure.Data.Configurations;

public class OrderFulfillmentLegConfiguration : IEntityTypeConfiguration<OrderFulfillmentLeg>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentLeg> builder)
    {
        builder.HasKey(l => l.LegId);
        builder.Property(l => l.LegId).ValueGeneratedNever();
    }
}