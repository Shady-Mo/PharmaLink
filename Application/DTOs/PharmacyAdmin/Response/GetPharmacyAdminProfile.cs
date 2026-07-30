using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PharmacyAdmin.Response
{
    public class GetPharmacyAdminProfile
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
    }
}
