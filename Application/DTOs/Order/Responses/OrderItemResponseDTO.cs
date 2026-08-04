namespace Application.DTOs.Order.Responses
{
    public class OrderItemResponseDTO
    {
        public Guid OrderItemId { get; set; }
        public Guid DrugId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? ArabicName { get; set; }
        public string? ImageUrl { get; set; }
        public int QuantityNeeded { get; set; }
        public ItemStatus ItemStatus { get; set; }
        
        public string Strength { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}
