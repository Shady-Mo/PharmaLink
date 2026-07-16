namespace Application.Errors
{
    public static class PharmacyErrors
    {
        public static readonly Error PharmacyNotFound =
        new("Pharmacy.PharmacyNotFound",
            "No Pharmacy was found for the provided ID.",
            StatusCodes.Status404NotFound);

        public static readonly Error Forbidden = new(
        "Pharmacy.Forbidden",
        "You are not allowed to access this Pharmacy.",
        StatusCodes.Status403Forbidden);

        public static readonly Error LicenseNumberNotUnique = new(
        "Pharmacy.LicenseNumberNotUnique",
        "A pharmacy with this license number already exists.",
        StatusCodes.Status400BadRequest);

        public static readonly Error InvalidOwnerUserId =
        new("Pharmacy.InvalidOwnerUserId",
            "Invalid OwnerUserId",
            StatusCodes.Status400BadRequest);
    }
}
