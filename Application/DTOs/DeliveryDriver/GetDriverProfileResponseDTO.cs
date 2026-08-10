using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.DeliveryDriver
{
    public class GetDriverProfileResponseDTO
    {
        public Guid DriverId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;

        public int TotalCompletedJobs { get; set; }
        public DateTime? LastLocationUpdateUtc { get; set; }
    }
}
