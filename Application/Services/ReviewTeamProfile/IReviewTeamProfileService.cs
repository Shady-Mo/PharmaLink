using Application.DTOs.ReviewTeam;

namespace Application.Services.ReviewTeamProfile;

public interface IReviewTeamProfileService
{
    Task<Result<ReviewTeamProfileResponseDto>> GetProfileAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<ReviewTeamProfileResponseDto>> UpdateProfileAsync(Guid userId, UpdateReviewTeamProfileDto updateDto,
        CancellationToken cancellationToken = default);

    Task<Result<ProfilePictureResponseDto>> UploadProfilePictureAsync(Guid userId,
        UploadProfilePictureForReviewTeamDto uploadDto, string baseUrl,
        CancellationToken cancellationToken = default);

    Task<Result<ProfilePictureResponseDto>> GetProfilePictureUrlAsync(Guid userId,
        CancellationToken cancellationToken = default);
}