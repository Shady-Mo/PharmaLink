namespace Application.DTOs.Dashboard.Responses;

public class DailySalesDTO
{
    public string Date { get; set; } = string.Empty;

    public decimal SalesAmount { get; set; }
}
