using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Supplier
{
    public class SupplierOrderDto
    {
        public Guid OrderId { get; set; }

        public string PharmacyBranchName { get; set; }

        public DateTime OrderedAt { get; set; }

        public string CurrentStatus { get; set; }

        public Guid DrugId { get; set; }
        public string DrugName { get; set; }
        public int RequestedQuantity { get; set; }
    }
}
