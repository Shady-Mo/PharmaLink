using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.Property(s => s.CompanyName)
                   .HasMaxLength(150)
                   .IsRequired(false);

            builder.Property(s => s.CommercialRegisterNumber)
                   .HasMaxLength(50)
                   .IsRequired(false);

        }
    }
}
