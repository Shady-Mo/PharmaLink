using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class PharmacyMissingStockLog
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public int DrugId { get; set; }
        public int QuantityRequested { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; } = false; // يتحول إلى true بعد المعالجة في التقرير

        // Navigation Properties
        public virtual Pharmacy Pharmacy { get; set; } = null!;
        public virtual Drug Drug { get; set; } = null!;
    }
}
