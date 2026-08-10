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

    public static readonly Error CannotChangeOwnRole =
        new("AdminUser.CannotChangeOwnRole",
            "You cannot change your own role.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidRole =
        new("AdminUser.InvalidRole",
            "The selected role is not supported.",
            StatusCodes.Status400BadRequest);

    public static readonly Error RoleUpdateFailed =
        new("AdminUser.RoleUpdateFailed",
            "Failed to update user role. Please try again later.",
            StatusCodes.Status500InternalServerError);
}
