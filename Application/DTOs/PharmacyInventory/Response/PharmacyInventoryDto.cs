using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PharmacyInventory.Response
{
    public class PharmacyInventoryDto
    {
        public Guid InventoryId { get; set; }
        public Guid BranchId { get; set; }

        public Guid DrugId { get; set; }

        public int StockQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public DateOnly? ExpiryDate { get; set; }
    }
}
