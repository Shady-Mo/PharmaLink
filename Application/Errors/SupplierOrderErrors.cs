using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class SupplierOrderErrors
    {
        public static readonly Error NotFound =
        new("SupplierOrderErrors.NotFound",
            "تعذّر العثور على طلب المورّد.",
            StatusCodes.Status404NotFound);

        public static readonly Error BadRequest =
        new("SupplierOrderErrors.BadRequest",
            "حالة غير صالحة.",
            StatusCodes.Status400BadRequest);

        public static readonly Error NoSuppliersFoundForDrug =
        new("SupplierOrderErrors.BadRequest",
            "لم يتم العثور على موردين للدواء.",
            StatusCodes.Status400BadRequest);

        public static readonly Error SupplierDoesNotHaveDrug =
            new("SupplierOrderErrors.BadRequest",
            "المورد لا يملك هذا الدواء.",
            StatusCodes.Status400BadRequest);
    }
}
