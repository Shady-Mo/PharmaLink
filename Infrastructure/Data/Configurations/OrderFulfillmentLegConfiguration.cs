namespace Infrastructure.Data.Configurations;

public class OrderFulfillmentLegConfiguration : IEntityTypeConfiguration<OrderFulfillmentLeg> {
    public void Configure(EntityTypeBuilder<OrderFulfillmentLeg> builder) {
        builder.HasKey(l => l.LegID);
    }
}
