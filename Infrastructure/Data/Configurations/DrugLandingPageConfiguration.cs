namespace Infrastructure.Data.Configurations;

public class DrugLandingPageConfiguration : IEntityTypeConfiguration<DrugLandingPage>
{
    public void Configure(EntityTypeBuilder<DrugLandingPage> builder)
    {
        builder.HasKey(l => l.Id);
    }
}
