namespace Infrastructure.Data.Configurations;

public class PharmacistConfiguration : IEntityTypeConfiguration<Pharmacist>
{
    public void Configure(EntityTypeBuilder<Pharmacist> builder)
    {
        builder.HasMany(a => a.Assignments)
            .WithOne(p => p.Pharmacist)
            .HasForeignKey(a => a.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}