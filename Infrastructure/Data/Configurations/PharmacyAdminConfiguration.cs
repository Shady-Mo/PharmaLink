namespace Infrastructure.Data.Configurations;

public class PharmacyAdminConfiguration : IEntityTypeConfiguration<PharmacyAdmin>
{
    public void Configure(EntityTypeBuilder<PharmacyAdmin> builder)
    {
        builder.HasIndex(a => a.PharmacyId)
            .HasDatabaseName("IX_PharmacyAdmins_PharmacyId");
    }
}
