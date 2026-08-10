namespace Domain.Entities;

public class DrugLandingPage
{
    public int Id { get; set; }
    
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = null!;

    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
