namespace Infrastructure.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(ci => ci.CartItemId);

        builder.Property(ci => ci.Quantity)
            .IsRequired();

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_CartItem_Quantity", "\"Quantity\" > 0"));

        builder.Property(ci => ci.UnitPriceSnapshot)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(ci => new { ci.CartId, ci.DrugId })
            .IsUnique();

        builder
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ci => ci.Drug)
            .WithMany(d => d.CartItems)
            .HasForeignKey(ci => ci.DrugId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
