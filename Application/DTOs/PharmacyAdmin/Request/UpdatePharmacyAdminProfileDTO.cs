using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PharmacyAdmin.Request
{
    public class UpdatePharmacyAdminProfileDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
