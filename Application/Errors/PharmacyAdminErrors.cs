using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class PharmacyAdminErrors
    {
        public static readonly Error PhoneAlreadyExists =
        new("PharmacyAdmin.PhoneAlreadyExists",
            "An account with this phone number already exists.",
            StatusCodes.Status409Conflict);

        public static readonly Error PharmacistNotFound =
        new("PharmacyAdmin.NotFound",
            "No Pharmacy Admin account was found for the provided ID.",
            StatusCodes.Status404NotFound);
    }
}
