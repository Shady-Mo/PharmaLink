using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PharmacyInventory.Request
{
    public class AdjustStockDTO
    {
        public AdjustmentType Type { get; set; }
        public int Quantity { get; set; }
    }

    public enum AdjustmentType
    {
        Increase,
        Decrease
    }
}
