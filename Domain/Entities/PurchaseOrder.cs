using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class PurchaseOrder
    {
        public Guid Id { get; set; }
        public Guid DrugId { get; set; }
        public Guid BranchId { get; set; }
        public int OrderedQuantity { get; set; }
        public POStatus Status { get; set; } = POStatus.PendingPharmacyApproval;
        public string AiRationale { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public Guid? SupplierId { get; set; } 
        public virtual Supplier? Supplier { get; set; }
        public virtual Drug Drug { get; set; }
        public virtual PharmacyBranch Branch { get; set; }
    }

    public enum POStatus
    {
        PendingPharmacyApproval = 0,
        RejectedByPharmacy = 1,
        SentToSupplier = 2,
        AcceptedBySupplier = 3,
        RejectedBySupplier = 4,
        ProcessingBySupplier = 5,
        Shipped = 6,
        Delivered = 7
    }
}
