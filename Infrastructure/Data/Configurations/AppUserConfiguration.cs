namespace Infrastructure.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder
            .Property(u => u.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasDiscriminator<string>("UserType")
            .HasValue<Patient>("Patient")
            .HasValue<Pharmacist>("Pharmacist")
            .HasValue<SystemAdmin>("SystemAdmin")
            .HasValue<PharmacyAdmin>("PharmacyAdmin")
            .HasValue<Supplier>("Supplier")
            .HasValue<DeliveryDriver>("DeliveryDriver");
    }
}