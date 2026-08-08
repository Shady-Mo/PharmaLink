using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class DeliveryJobConfiguration : IEntityTypeConfiguration<DeliveryJob>
    {
        public void Configure(EntityTypeBuilder<DeliveryJob> builder)
        {
            builder.HasKey(j => j.JobId);

            builder.HasOne(j => j.FulfillmentLeg)
             .WithMany()
             .HasForeignKey(j => j.LegId)
             .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(j => j.Driver)
             .WithMany(d => d.DeliveryJobs)
             .HasForeignKey(j => j.DriverId)
             .OnDelete(DeleteBehavior.SetNull);

            builder.Property(j => j.RowVersion)
             .IsRowVersion();
        }
    }
}
