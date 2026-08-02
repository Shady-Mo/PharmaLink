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
        public POStatus Status { get; set; } = POStatus.Pending;
        public string AiRationale { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }

        public virtual Drug Drug { get; set; }
        public virtual PharmacyBranch Branch { get; set; }
    }

    public enum POStatus
    {
        Pending,
        Approved,
        Submitted,
        Rejected
    }
}
