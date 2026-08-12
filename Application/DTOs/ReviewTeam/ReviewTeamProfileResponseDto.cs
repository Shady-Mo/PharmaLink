namespace Application.DTOs.ReviewTeam;

public class ReviewTeamProfileResponseDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}