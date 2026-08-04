namespace Infrastructure.Data.Configurations;

public class DrugCategoryConfiguration : IEntityTypeConfiguration<DrugCategory>
{
    public void Configure(EntityTypeBuilder<DrugCategory> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
