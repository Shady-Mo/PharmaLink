using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class PharmaciestError
    {
        public static readonly Error PharmaciestNotFound =
        new("Pharmaciest.UserNotFound",
            "No account was found for the provided user ID.",
            StatusCodes.Status404NotFound);
    }
}
