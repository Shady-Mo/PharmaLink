using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PreparationList.Response
{
    public class PreparationListDTO
    {
        public Guid OrderNumber { get; set; }
        public Guid LegId { get; set; }
        public string PatientName { get; set; }
        public List<MedcineDTO> MedcineDTOs { get; set; }
        public LegStatus Status { get; set; }
        public byte LegType { get; set; }
    }

    public class MedcineDTO
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
