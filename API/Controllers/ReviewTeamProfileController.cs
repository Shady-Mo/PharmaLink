using Application.DTOs.ReviewTeam;
using Application.Services.ReviewTeamProfile;

namespace API.Controllers;

[Authorize(Roles = AppRoles.PrescriptionReviewTeam)]
public class ReviewTeamProfileController(IReviewTeamProfileService reviewTeamProfileService) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ReviewTeamProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await reviewTeamProfileService.GetProfileAsync(User.GetUserId(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut]
    [ProducesResponseType(typeof(ReviewTeamProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateReviewTeamProfileDto updateDto,
        CancellationToken cancellationToken)
    {
        var result = await reviewTeamProfileService.UpdateProfileAsync(User.GetUserId(), updateDto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("picture")]
    [ProducesResponseType(typeof(ProfilePictureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProfilePicture(
        [FromForm] UploadProfilePictureForReviewTeamDto uploadDto,
        CancellationToken cancellationToken)
    {
        var result =
            await reviewTeamProfileService.UploadProfilePictureAsync(User.GetUserId(), uploadDto,
                $"{Request.Scheme}://{Request.Host}{Request.PathBase}",
                cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("picture")]
    [ProducesResponseType(typeof(ProfilePictureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfilePicture(CancellationToken cancellationToken)
    {
        var result = await reviewTeamProfileService.GetProfilePictureUrlAsync(User.GetUserId(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}