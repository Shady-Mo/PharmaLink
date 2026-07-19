namespace Application.DTOs;

/// <summary>
/// Base class for all paginated requests. Ensures pagination values are strictly within acceptable bounds.
/// </summary>
public class PaginatedRequest
{
    private int _pageNumber = 1;

    private int _pageSize = 10;

    /// <summary>
    /// The page number to retrieve. Automatically normalizes to a minimum of 1.
    /// </summary>
    /// <example>1</example>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// The number of records to return per page. Automatically bounded between 1 and 100.
    /// </summary>
    /// <example>10</example>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}