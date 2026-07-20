namespace Application.DTOs.Dashboard.Responses;

public class LowStockAlertDTO
{
    public int LowStockCount { get; set; }

    public int Threshold { get; set; }

    public bool RestockNeeded { get; set; }
}
