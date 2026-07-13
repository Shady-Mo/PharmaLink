namespace Infrastructure.Data.Configurations;

public class OrderFulfillmentLegStatusAuditConfiguration : IEntityTypeConfiguration<OrderFulfillmentLegStatusAudit>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentLegStatusAudit> builder)
    {
        builder.HasKey(a => a.AuditId);

        builder
            .Property(a => a.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder
            .Property(a => a.ChangedAtUtc)
            .IsRequired();

        builder
            .HasOne(a => a.Leg)
            .WithMany(l => l.StatusAudits)
            .HasForeignKey(a => a.LegId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(a => a.ChangedByUser)
            .WithMany()
            .HasForeignKey(a => a.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
