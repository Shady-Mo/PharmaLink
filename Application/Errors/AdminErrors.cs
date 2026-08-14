using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class AdminErrors
    {

        public static readonly Error AdminNotFound =
            new("Admin.NotFound",
                "تعذّر العثور على ملف تعريف المسؤول الموثّق.",
                StatusCodes.Status404NotFound);

        public static readonly Error PhoneAlreadyExists =
            new("Admin.PhoneAlreadyExists",
                "يوجد مسؤول مسجل بالفعل برقم الهاتف المحدد.",
                StatusCodes.Status409Conflict);
    }
}
