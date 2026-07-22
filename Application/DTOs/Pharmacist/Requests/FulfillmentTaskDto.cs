using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Pharmacist.Requests
{
    public class FulfillmentTaskDto
    {
        public string LegId { get; set; }
        public string OrderId { get; set; }
        public DateTime ReadyByEstimate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemsCount { get; set; }
    }
}
