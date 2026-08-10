using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.DeliveryDriver
{
    public class DeliveryJobHistoryDto
    {
        public Guid JobId { get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
