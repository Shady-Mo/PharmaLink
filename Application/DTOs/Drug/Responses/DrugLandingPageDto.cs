namespace Application.DTOs.Drug.Responses;

public class DrugLandingPageDto
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
