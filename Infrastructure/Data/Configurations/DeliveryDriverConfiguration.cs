using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class DeliveryDriverConfiguration : IEntityTypeConfiguration<DeliveryDriver>
    {
        public void Configure(EntityTypeBuilder<DeliveryDriver> builder)
        {
            builder.Property(d => d.CurrentLocation)
                .HasColumnType("geography");

        }
    }
}
