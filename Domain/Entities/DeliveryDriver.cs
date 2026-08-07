using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class DeliveryDriver : AppUser
    {
        public string VehicleInfo { get; set; } = string.Empty;

        public Point? CurrentLocation { get; set; }
        public DriverStatus DriverAvailability { get; set; } = DriverStatus.Offline;
        public DateTime LastLocationUpdateUtc { get; set; }
        public virtual ICollection<DeliveryJob> DeliveryJobs { get; set; } = new HashSet<DeliveryJob>();
    }

    public enum DriverStatus
    {
        Offline = 0,
        Available = 1,
        Busy = 2
    }
}
