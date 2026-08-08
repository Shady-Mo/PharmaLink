using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.DeliveryDriver
{
    public class UpdateDriverProfileRequestDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
    }
}
