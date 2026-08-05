using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Supplier
{
    public class SupplierDrugDto
    {
        public Guid DrugId { get; set; }
        public string BrandName { get; set; }
        public string GenericName { get; set; }
    }
}
