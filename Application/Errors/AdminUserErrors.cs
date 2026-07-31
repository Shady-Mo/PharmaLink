using Application.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Errors;

public static class AdminUserErrors
{
    public static readonly Error CannotDeactivateSelf =
        new("AdminUser.CannotDeactivateSelf",
            "You cannot deactivate your own account.",
            StatusCodes.Status400BadRequest);

    public static readonly Error NotFound =
        new("AdminUser.NotFound",
            "User not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error UpdateFailed =
        new("AdminUser.UpdateFailed",
            "Failed to update user status. Please try again later.",
            StatusCodes.Status500InternalServerError);
}
