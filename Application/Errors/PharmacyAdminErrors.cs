using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class PharmacyAdminErrors
    {
        public static readonly Error PhoneAlreadyExists =
        new("PharmacyAdmin.PhoneAlreadyExists",
            "رقم الهاتف هذا مستخدم بالفعل بحساب آخر.",
            StatusCodes.Status409Conflict);

        public static readonly Error PharmacistNotFound =
        new("PharmacyAdmin.NotFound",
            "لا يوجد حساب مدير صيدلية مرتبط بهذا المعرّف.",
            StatusCodes.Status404NotFound);
    }
}
