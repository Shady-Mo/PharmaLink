using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Pharmacy.Responses
{
    public class AdminPharmacyDetailDTO
    {
        public Guid PharmacyId { get; set; }
        public string LegalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
        public int BranchesCount { get; set; }
        public int DrugsCount { get; set; }
        public PharmacyOwnerDTO? Owner { get; set; }
        public List<AdminPharmacyBranchDTO> Branches { get; set; } = [];
    }
}
