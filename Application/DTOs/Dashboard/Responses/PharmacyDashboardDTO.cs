namespace Application.DTOs.Dashboard.Responses;

public class PharmacyDashboardDTO
{
    public List<BranchesDTO> Branches { get; set; }

    public PharmacyKpiDTO Kpis { get; set; } = null!;

    public LowStockAlertDTO LowStockAlert { get; set; } = null!;

    public ICollection<DailySalesDTO> SalesTrend { get; set; } = new List<DailySalesDTO>();

    public ICollection<PharmacyRecentOrderDTO> RecentOrders { get; set; } = new List<PharmacyRecentOrderDTO>();
}
