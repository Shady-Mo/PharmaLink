using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Supplier
{
    public class SupplierProfileDto
    {
        public Guid SupplierId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
