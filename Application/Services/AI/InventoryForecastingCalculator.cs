namespace Application.Services.AI
{
    public class InventoryForecastingCalculator : IInventoryForecastingCalculator
    {
        public double CalculateAverageDailyDemand(int totalSales, int days, double seasonalityFactor = 1.0, double trendFactor = 1.0)
        {
            if (days <= 0) return 0;

            double baseAdd = (double)totalSales / days;

            // تطبيق تأثير المواسم والتريند (بناءً على طلبات الـ AI)
            return baseAdd * seasonalityFactor * trendFactor;
        }

        public int CalculateReorderPoint(double averageDailyDemand, int leadTimeInDays, int safetyStock)
        {
            // ROP = (ADD * LeadTime) + SafetyStock
            double rop = (averageDailyDemand * leadTimeInDays) + safetyStock;

            // بنعمل Round up عشان نضمن إننا في الأمان
            return (int)Math.Ceiling(rop);
        }

        public DateTime? CalculateStockDepletionDate(int currentStock, double averageDailyDemand)
        {
            if (averageDailyDemand <= 0) return null; // مفيش استهلاك، فالمخزون مش هيخلص

            double daysUntilDepletion = currentStock / averageDailyDemand;

            return DateTime.UtcNow.AddDays(daysUntilDepletion);
        }

        public int CalculateEconomicOrderQuantity(double averageDailyDemand, double orderCost, double holdingCostPerUnit)
        {
            if (holdingCostPerUnit <= 0) return 0;

            double annualDemand = averageDailyDemand * 365;

            // معادلة EOQ = Sqrt((2 * D * S) / H)
            // D = Annual Demand
            // S = Cost per order
            // H = Holding cost per unit per year
            double eoq = Math.Sqrt((2 * annualDemand * orderCost) / holdingCostPerUnit);

            return (int)Math.Ceiling(eoq);
        }
    }
}
