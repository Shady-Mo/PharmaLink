using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class SupplierDrug
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public Guid DrugId { get; set; }
        public virtual Drug Drug { get; set; }

        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
