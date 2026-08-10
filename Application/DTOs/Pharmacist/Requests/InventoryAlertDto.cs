using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Pharmacist.Requests
{
    public class InventoryAlertDto
    {
        public string DrugId { get; set; }
        public string BrandName { get; set; }
        public int StockQuantity { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public string AlertType { get; set; }
    }
}
