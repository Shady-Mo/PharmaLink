namespace Application.DTOs.OrderFulfillmentLeg.Responses
{
    public class BranchOrderRowDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string DrugsSummary { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public LegStatus Status { get; set; }
        public DateTime Date { get; set; }
    }
}
