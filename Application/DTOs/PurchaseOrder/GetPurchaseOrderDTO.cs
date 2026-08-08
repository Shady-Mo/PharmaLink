using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PurchaseOrder
{
    public class GetPurchaseOrderDTO
    {
        public Guid Id { get; set; }
        public Guid DrugId { get; set; }
        public int OrderedQuantity { get; set; }
        public POStatus Status { get; set; }
        public string AiRationale { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DrugName { get; set; }
        public string BranchName { get; set; }
    }
}
