using System;

namespace Application.DTOs.Geo
{
    public class NearbyBranchResult
    {
        public Guid BranchID { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public bool SupportsDelivery { get; set; }
        public bool SupportsPickup { get; set; }
    }
}