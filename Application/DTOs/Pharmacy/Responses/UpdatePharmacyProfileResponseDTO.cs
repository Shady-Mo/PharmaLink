using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Pharmacy.Responses
{
    public class UpdatePharmacyProfileResponseDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
