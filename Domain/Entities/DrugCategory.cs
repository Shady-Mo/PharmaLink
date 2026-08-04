namespace Domain.Entities;

public class DrugCategory
{
    public int Id { get; set; }
    
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; } = string.Empty;
    public int Level { get; set; }

    public int? ParentId { get; set; }
    public DrugCategory? Parent { get; set; }
    public ICollection<DrugCategory> SubCategories { get; set; } = new List<DrugCategory>();
    
    public ICollection<Drug> Drugs { get; set; } = new List<Drug>();
}
