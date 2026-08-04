using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Configurations
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.HasOne(b => b.Drug)
                .WithMany(d => d.PurchaseOrders)
                .HasForeignKey(b => b.DrugId);

            builder.HasOne(b => b.Branch)
                .WithMany(d => d.PurchaseOrders)
                .HasForeignKey(b => b.BranchId);

            builder.HasOne(po => po.Supplier)
                   .WithMany(s => s.PurchaseOrders)
                   .HasForeignKey(po => po.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
