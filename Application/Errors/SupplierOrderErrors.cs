using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class SupplierOrderErrors
    {
        public static readonly Error NotFound =
        new("SupplierOrderErrors.NotFound",
            "Supplier Order not found.",
            StatusCodes.Status404NotFound);

        public static readonly Error BadRequest =
        new("SupplierOrderErrors.BadRequest",
            "Invalid Status.",
            StatusCodes.Status400BadRequest);

        public static readonly Error NoSuppliersFoundForDrug =
        new("SupplierOrderErrors.BadRequest",
            "No Suppliers Found For Drug.",
            StatusCodes.Status400BadRequest);

        public static readonly Error SupplierDoesNotHaveDrug =
            new("SupplierOrderErrors.BadRequest",
            "Supplier Does Not Have Drug.",
            StatusCodes.Status400BadRequest);
            }
}
