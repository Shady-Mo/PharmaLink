using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class SupplierDrugConfiguration : IEntityTypeConfiguration<SupplierDrug>
    {
        public void Configure(EntityTypeBuilder<SupplierDrug> builder)
        {
            builder.HasKey(sd => sd.Id);

            builder.HasIndex(sd => new { sd.SupplierId, sd.DrugId })
                   .IsUnique();

            builder.Property(sd => sd.UnitPrice)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(sd => sd.IsActive)
                   .HasDefaultValue(true);

            builder.HasOne(sd => sd.Supplier)
                   .WithMany(s => s.SupplierDrugs)
                   .HasForeignKey(sd => sd.SupplierId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sd => sd.Drug)
                   .WithMany()
                   .HasForeignKey(sd => sd.DrugId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
