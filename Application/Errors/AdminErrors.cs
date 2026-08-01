using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class AdminErrors
    {

        public static readonly Error AdminNotFound =
            new("Admin.NotFound",
                "The authenticated Admin profile was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error PhoneAlreadyExists =
            new("Admin.PhoneAlreadyExists",
                "An admin with the specified phone number already exists.",
                StatusCodes.Status409Conflict);
    }
}
