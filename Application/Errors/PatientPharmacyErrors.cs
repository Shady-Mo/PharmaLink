namespace Application.Errors;

public static class PatientPharmacyErrors
{
    public static readonly Error InvalidCoordinates = new(
        "PatientPharmacy.InvalidCoordinates",
        "Latitude must be between -90 and 90, and Longitude must be between -180 and 180.",
        StatusCodes.Status400BadRequest);

    public static readonly Error NoPharmaciesFound = new(
        "PatientPharmacy.NoPharmaciesFound",
        "No pharmacy branches were found within the specified radius.",
        StatusCodes.Status404NotFound);
}
