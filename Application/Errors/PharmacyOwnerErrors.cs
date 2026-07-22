using Microsoft.AspNetCore.Http;

namespace Application.Errors
{
    public static class PharmacyOwnerErrors
    {
        public static readonly Error PharmacyOwnerNotFound =
            new("PharmacyOwner.NotFound",
                "No pharmacy owner account was found for the provided ID.",
                StatusCodes.Status404NotFound);

        public static readonly Error EmailAlreadyExists =
            new("PharmacyOwner.EmailAlreadyExists",
                "An account with this email address already exists.",
                StatusCodes.Status409Conflict);

        public static readonly Error PhoneAlreadyExists =
            new("PharmacyOwner.PhoneAlreadyExists",
                "An account with this phone number already exists.",
                StatusCodes.Status409Conflict);

        public static readonly Error RegistrationFailed =
            new("PharmacyOwner.RegistrationFailed",
                "Failed to create the pharmacy owner account due to validation or server errors.",
                StatusCodes.Status500InternalServerError);

        public static readonly Error RoleAssignmentFailed =
            new("PharmacyOwner.RoleAssignmentFailed",
                "Failed to assign the Pharmacy Admin role to the owner account.",
                StatusCodes.Status500InternalServerError);

        public static readonly Error InvalidUserRole =
            new("PharmacyOwner.InvalidUserRole",
                "The specified user is not registered as a Pharmacy Admin and cannot be assigned as an owner.",
                StatusCodes.Status400BadRequest);

        public static readonly Error OwnerNotActive =
            new("PharmacyOwner.OwnerNotActive",
                "The owner user account is not active and is not eligible to be a pharmacy owner.",
                StatusCodes.Status400BadRequest);

        public static readonly Error PharmacyNotEligible =
            new("PharmacyOwner.PharmacyNotEligible",
                "The target pharmacy is deleted or rejected and cannot have an assigned owner.",
                StatusCodes.Status400BadRequest);
    }
}
