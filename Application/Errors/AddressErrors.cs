using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{

    public static class AddressErrors
    {
        public static readonly Error NotFound =
            new("Address.NotFound", "Address not found.", StatusCodes.Status404NotFound);

        public static readonly Error Forbidden =
            new("Address.Forbidden",
                "You do not have permission to access this address.",
                StatusCodes.Status403Forbidden);

        public static readonly Error AuditReasonRequired =
            new("Address.AuditReasonRequired",
                "A reason is required when a System Admin accesses a patient's address.",
                StatusCodes.Status400BadRequest);
    }
}
