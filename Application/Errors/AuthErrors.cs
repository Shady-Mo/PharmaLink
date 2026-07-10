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

    public static readonly Error AccountSuspended =
    new("Auth.AccountSuspended",
        "This account has been suspended.",
        StatusCodes.Status403Forbidden);
}
