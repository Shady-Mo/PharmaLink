using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Pharmacy.Responses
{
    public class GetPharmacyProfileResponseDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public List<PharmacyDTO> AdministeredPharmacies { get; set; }

    }

    public class PharmacyDTO
    {
        public Guid PharmacyId { get; set; }
        public Guid OwnerUserId { get; set; }
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;

        public VerificationStatus VerificationStatus { get; set; }
    }
}
