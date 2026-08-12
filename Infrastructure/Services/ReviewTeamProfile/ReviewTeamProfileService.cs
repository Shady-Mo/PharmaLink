using Application.DTOs.ReviewTeam;
using Application.Services.ReviewTeamProfile;

namespace Infrastructure.Services.ReviewTeamProfile;

public class ReviewTeamProfileService(
    UserManager<AppUser> userManager,
    ILogger<ReviewTeamProfileService> logger,
    IWebHostEnvironment env) : IReviewTeamProfileService
{
    public async Task<Result<ReviewTeamProfileResponseDto>> GetProfileAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure<ReviewTeamProfileResponseDto>(new Error("ReviewTeam.NotFound",
                "Review team member not found.", 404));
        }

        var dto = new ReviewTeamProfileResponseDto
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl
        };

        return Result.Success(dto);
    }

    public async Task<Result<ReviewTeamProfileResponseDto>> UpdateProfileAsync(Guid userId,
        UpdateReviewTeamProfileDto updateDto, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return Result.Failure<ReviewTeamProfileResponseDto>(new Error("ReviewTeam.NotFound",
                "Review team member not found.", 404));
        }

        user.FullName = updateDto.FullName;
        user.PhoneNumber = updateDto.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to update review team profile for user {UserId}. Errors: {Errors}", userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure<ReviewTeamProfileResponseDto>(new Error("ReviewTeam.UpdateFailed",
                "Failed to update profile.", 400));
        }

        var responseDto = new ReviewTeamProfileResponseDto
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl
        };

        return Result.Success(responseDto);
    }

    public async Task<Result<ProfilePictureResponseDto>> UploadProfilePictureAsync(Guid userId,
        UploadProfilePictureForReviewTeamDto uploadDto, string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure<ProfilePictureResponseDto>(new Error("ReviewTeam.NotFound",
                "Review team member not found.", 404));
        }

        if (uploadDto.File.Length == 0)
        {
            return Result.Failure<ProfilePictureResponseDto>(new Error("File.Empty", "File cannot be empty.", 400));
        }

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var absolutePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await uploadDto.File.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"uploads/profiles/{uniqueFileName}";
        var fullUrl = $"{baseUrl.TrimEnd('/')}/{relativePath}";

        // Delete old picture if it exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            try
            {
                var oldRelativePath = user.ProfilePictureUrl;
                if (Uri.TryCreate(user.ProfilePictureUrl, UriKind.Absolute, out var uri))
                {
                    oldRelativePath = uri.AbsolutePath.TrimStart('/');
                }

                var oldAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldRelativePath);
                if (File.Exists(oldAbsolutePath))
                {
                    File.Delete(oldAbsolutePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old profile picture for user {UserId}", userId);
            }
        }

        user.ProfilePictureUrl = fullUrl;
        var updateResult = await userManager.UpdateAsync(user);

        if (updateResult.Succeeded)
            return Result.Success(new ProfilePictureResponseDto { ProfilePictureUrl = fullUrl });

        if (File.Exists(absolutePath)) File.Delete(absolutePath);

        return Result.Failure<ProfilePictureResponseDto>(new Error("ReviewTeam.UpdateFailed",
            "Failed to update profile picture URL in database.", 400));
    }

    public async Task<Result<ProfilePictureResponseDto>> GetProfilePictureUrlAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return Result.Failure<ProfilePictureResponseDto>(new Error("ReviewTeam.NotFound",
                "Review team member not found.", 404));
        }

        return Result.Success(new ProfilePictureResponseDto
            { ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty });
    }
}