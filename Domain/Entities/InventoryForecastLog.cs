using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class InventoryForecastLog
    {
        public Guid Id { get; set; }
        public Guid DrugId { get; set; }
        public Guid BranchId { get; set; }
        public DateTime ForecastDate { get; set; } = DateTime.UtcNow;
        public double AverageDailyDemand { get; set; }
        public int ReorderPoint { get; set; }
        public int PredictedDemand { get; set; }
        public DateTime? PredictedStockoutDate { get; set; }
        public string ActionTaken { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string AiRationale { get; set; }
    }
}
