namespace Infrastructure.Data.Configurations;

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.HasKey(d => d.DrugId);

        builder.Property(d => d.BrandName)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(d => d.ArabicName)
            .HasMaxLength(2000)
            .UseCollation("Arabic_CI_AI");

        builder.Property(d => d.Manufacturer)
            .HasMaxLength(2000);

        builder.Property(d => d.Form)
            .HasMaxLength(2000);

        builder.Property(d => d.Price)
            .HasPrecision(18, 2);
            
        builder.Property(d => d.FinalPrice)
            .HasPrecision(18, 2);

        builder.Property(d => d.Discount)
            .HasPrecision(18, 2);

        builder.Property(d => d.CostPrice)
            .HasPrecision(18, 2);

        builder.HasOne(d => d.Category)
            .WithMany(c => c.Drugs)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.Inventories)
            .WithOne(i => i.Drug)
            .HasForeignKey(i => i.DrugId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.OrderItems)
            .WithOne(oi => oi.Drug)
            .HasForeignKey(oi => oi.DrugId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Suppliers)
            .WithOne(s => s.Drug)
            .HasForeignKey(s => s.DrugId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.LandingPages)
            .WithOne(lp => lp.Drug)
            .HasForeignKey(lp => lp.DrugId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(d => d.ArabicName)
            .UseCollation("Arabic_CI_AI");
    }
}