namespace Application.Errors
{
    public static class PharmacyOrderErrors
    {
        public static readonly Error PharmacyContextMissing = new(
            "PharmacyOrder.PharmacyContextMissing",
            "The authenticated user is not associated with any pharmacy.",
            StatusCodes.Status403Forbidden);

        public static readonly Error BranchContextMissing = new(
            "PharmacyOrder.BranchContextMissing",
            "The authenticated user is not associated with this branch.",
            StatusCodes.Status403Forbidden);

        public static readonly Error OrderNotFound = new(
            "PharmacyOrder.NotFound",
            "No order was found for the provided ID within your pharmacy.",
            StatusCodes.Status404NotFound);
    }
}
