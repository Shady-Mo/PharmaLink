namespace Application.Errors;

public static class DashboardErrors
{
    public static readonly Error PharmacyContextMissing = new(
        "Dashboard.PharmacyContextMissing",
        "No pharmacy is associated with the authenticated user.",
        StatusCodes.Status403Forbidden);

    public static readonly Error BranchContextMissing = new(
        "Dashboard.BranchContextMissing",
        "No branch is associated with the authenticated user.",
        StatusCodes.Status403Forbidden);

    public static readonly Error PharmacyNotFound = new(
        "Dashboard.PharmacyNotFound",
        "The pharmacy associated with the authenticated user was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error BranchNotFound = new(
        "Dashboard.BranchNotFound",
        "The branch associated with the authenticated user was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error PharmacyRetrievalFailed = new(
        "Dashboard.PharmacyRetrievalFailed",
        "Failed to retrieve the pharmacy dashboard.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error BranchRetrievalFailed = new(
        "Dashboard.BranchRetrievalFailed",
        "Failed to retrieve the branch dashboard.",
        StatusCodes.Status500InternalServerError);
}
