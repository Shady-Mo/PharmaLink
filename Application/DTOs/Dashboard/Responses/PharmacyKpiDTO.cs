namespace Application.DTOs.Dashboard.Responses;

public class PharmacyKpiDTO
{
    public int TotalMedicines { get; set; }

    public int LowStockMedicinesCount { get; set; }

    public int TodaysOrdersCount { get; set; }

    public decimal? TodaysOrdersChangePercent { get; set; }

    public decimal MonthlyRevenue { get; set; }

    public decimal? MonthlyRevenueChangePercent { get; set; }
}
