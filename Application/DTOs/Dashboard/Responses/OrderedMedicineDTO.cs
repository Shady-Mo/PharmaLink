namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Represents a medicine included in an order.
/// </summary>
public class OrderedMedicineDTO
{
    /// <summary>
    /// Unique identifier of the medicine/drug.
    /// </summary>
    public Guid DrugId { get; set; }

    /// <summary>
    /// Name of the medicine.
    /// </summary>
    public string DrugName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; set; }
}
