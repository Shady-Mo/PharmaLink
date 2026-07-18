namespace Application.Errors;

public static class PharmacistErrors
{
    public static readonly Error PharmacistNotFound =
        new("Pharmacist.NotFound",
            "No pharmacist account was found for the provided ID.",
            StatusCodes.Status404NotFound);

    public static readonly Error EmailAlreadyExists =
        new("Pharmacist.EmailAlreadyExists",
            "An account with this email address already exists.",
            StatusCodes.Status409Conflict);

    public static readonly Error PhoneAlreadyExists =
        new("Pharmacist.PhoneAlreadyExists",
            "An account with this phone number already exists.",
            StatusCodes.Status409Conflict);

    public static readonly Error RegistrationFailed =
        new("Pharmacist.RegistrationFailed",
            "Failed to create the pharmacist account due to a server error. Please try again.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error AlreadyAssigned =
        new("Pharmacist.AlreadyAssigned",
            "This pharmacist already has an active assignment. Use the reassign endpoint to change their pharmacy.",
            StatusCodes.Status409Conflict);

    public static readonly Error NoActiveAssignment =
        new("Pharmacist.NoActiveAssignment",
            "This pharmacist does not have an active assignment to terminate.",
            StatusCodes.Status404NotFound);

    public static readonly Error PharmacyNotFound =
        new("Pharmacist.PharmacyNotFound",
            "The target pharmacy was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error AdminNotFound =
        new("Pharmacist.AdminNotFound",
            "The authenticated admin account could not be resolved.",
            StatusCodes.Status401Unauthorized);

    public static readonly Error AdminNotAssignedToPharmacy =
        new("Pharmacist.AdminNotAssigned",
            "The authenticated admin is not assigned to any pharmacy.",
            StatusCodes.Status403Forbidden);
}
