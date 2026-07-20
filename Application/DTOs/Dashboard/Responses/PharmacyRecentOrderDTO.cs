namespace Application.DTOs.Dashboard.Responses;

public class PharmacyRecentOrderDTO
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public int OrderedMedicinesCount { get; set; }

    public string Summary { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public string OrderStatusLabel { get; set; } = string.Empty;
}
