namespace Application.Services.AI;

public class ExtractedMedicineItem
{
    public string MedicineName { get; set; } = string.Empty;
    public string? OriginalMedicineName { get; set; }
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Dose { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Route { get; set; }
    public double? Confidence { get; set; }
}