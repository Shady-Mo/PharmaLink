namespace Application.DTOs.Drug.Requests;

/// <summary>
/// Represents the filtering, sorting, and pagination options used to retrieve drugs.
/// </summary>
public class DrugSearchRequest : PaginatedRequest
{
    /// <summary>
    /// A partial or full drug name used for searching.
    /// Searches both the generic name and brand name.
    /// </summary>
    /// <example>Paracetamol</example>
    public string? SearchValue { get; init; }

    /// <summary>
    /// Filters drugs by dosage form.
    /// </summary>
    /// <example>ORAL.SOLID</example>
    public string? Form { get; init; }

    /// <summary>
    /// The property used for sorting.
    /// </summary>
    /// <example>BrandName</example>
    public string? SortColumn { get; init; }

    /// <summary>
    /// The sorting direction.
    /// Allowed values are ASC and DESC.
    /// </summary>
    /// <example>ASC</example>
    public string? SortDirection { get; init; } = "ASC";
}