namespace Application.DTOs.OrderRouting;

public sealed record CartItemDto
{
    public Guid DrugId { get; init; }

    public string DrugName { get; init; } = string.Empty;
    
    public string DrugNameAr { get; init; } = string.Empty;

    public int Quantity { get; init; }
}
