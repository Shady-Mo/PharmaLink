namespace Application.Settings;

public class OrderFulfillmentSettings
{
    public const string SectionName = "OrderFulfillment";

    public int EstimatedPreparationMinutes { get; set; } = 30;
}
