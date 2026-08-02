using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Notification
{
    public class PoNotificationDto
    {
        public Guid BranchId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public DateTime? PredictedStockoutDate { get; set; }
        public int RecommendedOrderQuantity { get; set; }
        public string AiRationale { get; set; } = string.Empty;
    }
}
