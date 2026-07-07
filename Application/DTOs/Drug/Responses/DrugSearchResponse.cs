namespace Application.DTOs.Drug.Responses;

public class DrugSearchResponse
{
    public Guid DrugId { get; set; }
    public string GenericName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
}
