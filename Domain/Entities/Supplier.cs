using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Supplier: AppUser
    {
        public string? CompanyName { get; set; }
        public string? CommercialRegisterNumber { get; set; }

        public virtual ICollection<SupplierDrug> SupplierDrugs { get; set; } = new List<SupplierDrug>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
