using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class SupplierDrugErrors
    {
        public static readonly Error NotFound =
       new("SupplierOrderErrors.NotFound",
           "Supplier Order not found.",
           StatusCodes.Status404NotFound);

        public static readonly Error BadRequest =
        new("SupplierOrderErrors.BadRequest",
            "Bad Request",
            StatusCodes.Status400BadRequest);
    }
}
