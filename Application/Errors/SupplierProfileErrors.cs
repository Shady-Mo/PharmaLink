using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class SupplierProfileErrors
    {
        public static readonly Error NotFound =
        new("SupplierProfileErrors.NotFound",
            "طلب المورّد غير موجود في هذا الملف الشخصي.",
            StatusCodes.Status404NotFound);

        public static readonly Error BadRequest =
        new("Supplier Profile Errors.BadRequest",
            "Bad Request",
            StatusCodes.Status400BadRequest);
    }
}
