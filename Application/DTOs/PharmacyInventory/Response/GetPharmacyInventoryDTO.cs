using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PharmacyInventory.Response
{
    public class GetPharmacyInventoryDTO
    {
        public Guid InventoryId { get; set; }
        public Guid BranchId { get; set; }

        public Guid DrugId { get; set; }
        public string ArabicName { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

        public int ReservedQuantity { get; set; }
        public decimal UnitPrice { get; set; }

        public DateOnly ExpiryDate { get; set; }
    }
}
