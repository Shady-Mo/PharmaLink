namespace Application.DTOs.Drug.Responses;

public class DrugCategoryDto
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; } = string.Empty;
    public int Level { get; set; }
    public int? ParentId { get; set; }
    public ICollection<DrugCategoryDto> SubCategories { get; set; } = new List<DrugCategoryDto>();
}
