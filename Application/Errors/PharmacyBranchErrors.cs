namespace Application.Errors
{
    public static class PharmacyBranchErrors
    {
        public static readonly Error BranchNotFound = new(
            "PharmacyBranch.NotFound",
            "No branch was found for the provided ID within your pharmacy.",
            StatusCodes.Status404NotFound);

        public static readonly Error PharmacyContextMissing = new(
            "PharmacyBranch.PharmacyContextMissing",
            "The authenticated user is not associated with any pharmacy.",
            StatusCodes.Status403Forbidden);

        public static readonly Error DuplicateBranchName = new(
            "PharmacyBranch.DuplicateName",
            "A branch with this name already exists in your pharmacy.",
            StatusCodes.Status400BadRequest);

        public static readonly Error InvalidCoordinates = new(
            "PharmacyBranch.InvalidCoordinates",
            "Latitude and Longitude must both be provided together and within valid ranges.",
            StatusCodes.Status400BadRequest);
    }
}
