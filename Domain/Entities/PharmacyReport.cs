using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class PharmacyReport
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public string ReportTitle { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDownloaded { get; set; } = false; 

        // Navigation Property
        public virtual Pharmacy Pharmacy { get; set; } = null!;
    }
}
