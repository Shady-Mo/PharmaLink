using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Pharmacist.Requests
{
    public class PharmacistDailyMetricsDto
    {
        public int PendingPrescriptionReviews { get; set; }
        public int CompletedReviewsToday { get; set; }
        public int PendingFulfillmentOrders { get; set; }
        public int CompletedOrdersToday { get; set; }
    }
}
