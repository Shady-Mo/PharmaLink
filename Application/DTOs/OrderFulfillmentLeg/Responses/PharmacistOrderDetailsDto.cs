using Domain.Enums;
using Application.DTOs.Order.Responses;

namespace Application.DTOs.OrderFulfillmentLeg.Responses;

public class PharmacistOrderDetailsDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public FulfillmentMode FulfillmentMode { get; set; }

    public PharmacistOrderPatientDto Patient { get; set; } = null!;
    
    public List<PharmacistOrderItemDto> Items { get; set; } = new();
    
    public string? Notes { get; set; }
    
    public OrderFulfillmentLegDto? AssignedLeg { get; set; }
}

public class PharmacistOrderPatientDto
{
    public Guid PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class PharmacistOrderItemDto
{
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public string Strength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
}
