using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PrescriptionReview.Responses
{
    public class MedicineSearchDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
