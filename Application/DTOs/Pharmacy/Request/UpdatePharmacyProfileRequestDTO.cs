using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Pharmacy.Request
{
    public class UpdatePharmacistProfileRequestDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

    }
}
