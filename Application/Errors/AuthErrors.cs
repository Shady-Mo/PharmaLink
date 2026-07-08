using Application.DTOs.Auth.Requests;
using Application.DTOs.Auth.Responses;
using Application.Errors;

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
}
