namespace Infrastructure.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(a => a.AddressId);

        //builder
        //    .Property(a => a.Label)
        //    .HasMaxLength(100);

        builder
            .Property(a => a.AddressLine).HasMaxLength(500).IsRequired();

        builder
            .Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(a => a.Governorate)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasMany(a => a.Deliveries)
            .WithOne(o => o.DeliveryAddress)
            .HasForeignKey(o => o.DeliveryAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}