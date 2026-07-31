namespace Infrastructure.Data.Configurations;

public class PharmacyBranchScheduleConfiguration : IEntityTypeConfiguration<PharmacyBranchSchedule>
{
    public void Configure(EntityTypeBuilder<PharmacyBranchSchedule> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Day)
            .HasConversion<int>()
            .IsRequired();

        // Each branch can have at most one schedule row per day
        builder.HasIndex(s => new { s.BranchId, s.Day }).IsUnique();

        builder.HasOne(s => s.Branch)
            .WithMany(b => b.WorkingSchedule)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
