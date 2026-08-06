using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.DeliveryDriver
{
    public class DeliveryJobNotificationDto
    {
        public Guid JobId { get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public double DistanceKm { get; set; }
    }
}
