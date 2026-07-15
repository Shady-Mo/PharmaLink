using System;
using System.Collections.Generic;

namespace Application.DTOs.PrescriptionReview.Requests;

public class UpdatePrescriptionReviewDTO
{
    public List<UpdateMedicineItemDTO> Medicines { get; set; } = [];
}

public class UpdateMedicineItemDTO
{
    public Guid? PrescriptionReviewMedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Dose { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Route { get; set; }
}
