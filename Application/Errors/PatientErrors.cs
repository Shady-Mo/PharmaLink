namespace Application.Errors;

public static class PatientErrors
{
    public static readonly Error PatientNotFound =
        new("Patient.NotFound",
            "The authenticated patient profile was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error PhoneAlreadyExists =
        new("Patient.PhoneAlreadyExists",
            "A patient with the specified phone number already exists.",
            StatusCodes.Status409Conflict);
}