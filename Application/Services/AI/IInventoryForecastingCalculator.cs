using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.AI
{
    public interface IInventoryForecastingCalculator
    {
        double CalculateAverageDailyDemand(int totalSales, int days, double seasonalityFactor = 1.0, double trendFactor = 1.0);
        int CalculateReorderPoint(double averageDailyDemand, int leadTimeInDays, int safetyStock);
        DateTime? CalculateStockDepletionDate(int currentStock, double averageDailyDemand);
        int CalculateEconomicOrderQuantity(double averageDailyDemand, double orderCost, double holdingCostPerUnit);
    }
}
