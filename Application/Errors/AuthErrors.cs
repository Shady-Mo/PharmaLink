namespace Application.Errors;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists =
        new("Auth.EmailAlreadyExists",
            "An account with this email address already exists.",
            StatusCodes.Status409Conflict);

    public static readonly Error PhoneAlreadyExists =
        new("Auth.PhoneAlreadyExists",
            "An account with this phone number already exists.",
            StatusCodes.Status409Conflict);

    public static readonly Error RegistrationFailed =
        new("Auth.RegistrationFailed",
            "Account registration failed due to a server error. Please try again.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", 
        "The email or password is incorrect.", 
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidEmail = new("Auth.InvalidEmails",
        "The email is incorrect.",
        StatusCodes.Status404NotFound);

    public static readonly Error AccountSuspended =
    new("Auth.AccountSuspended",
        "This account has been suspended.",
        StatusCodes.Status403Forbidden);

    public static readonly Error PhoneNotVerified =
        new("Auth.PhoneNotVerified",
            "Your phone number has not been verified. Please verify your phone number before logging in.",
            StatusCodes.Status403Forbidden);

    public static readonly Error TokenError =
       new("Auth.TokenError",
           "The Token is invalid.",
           StatusCodes.Status400BadRequest);

    public static readonly Error CurrentPasswordIncorrect =
        new("Auth.CurrentPasswordIncorrect",
            "The current password is incorrect.",
            StatusCodes.Status401Unauthorized);

    public static readonly Error PasswordChangeRequired =
        new("Auth.PasswordChangeRequired",
            "Password change failed due to a server error. Please try again.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error UserNotFound =
        new("Auth.UserNotFound",
            "User not found.",
            StatusCodes.Status404NotFound);
}
