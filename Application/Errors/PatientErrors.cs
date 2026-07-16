namespace Application.Errors;

public static class PatientErrors
{
    public static readonly Error PatientNotFound = new("Patient.NotFound", "The authenticated patient profile was not found.", StatusCodes.Status404NotFound);
}