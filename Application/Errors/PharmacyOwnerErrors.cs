using Microsoft.AspNetCore.Http;

namespace Application.Errors
{
    public static class PharmacyOwnerErrors
    {
        public static readonly Error PharmacyOwnerNotFound =
            new("PharmacyOwner.NotFound",
                "لا يوجد حساب مالك صيدلية مرتبط بهذا المعرّف.",
                StatusCodes.Status404NotFound);

        public static readonly Error EmailAlreadyExists =
            new("PharmacyOwner.EmailAlreadyExists",
                "البريد الإلكتروني هذا مستخدم بالفعل بحساب آخر.",
                StatusCodes.Status409Conflict);

        public static readonly Error PhoneAlreadyExists =
            new("PharmacyOwner.PhoneAlreadyExists",
                "رقم الهاتف هذا مستخدم بالفعل بحساب آخر.",
                StatusCodes.Status409Conflict);

        public static readonly Error RegistrationFailed =
            new("PharmacyOwner.RegistrationFailed",
                "فشل إنشاء حساب مالك الصيدلية بسبب أخطاء في التحقق من صحة البيانات أو خطأ في الخادم.",
                StatusCodes.Status500InternalServerError);

        public static readonly Error RoleAssignmentFailed =
            new("PharmacyOwner.RoleAssignmentFailed",
                "تعذّر تعيين صلاحية مدير صيدلية لحساب المالك.",
                StatusCodes.Status500InternalServerError);

        public static readonly Error InvalidUserRole =
            new("PharmacyOwner.InvalidUserRole",
                "المستخدم المحدد ليس لديه صلاحية مدير صيدلية، لذا لا يمكن إسناده كمالك.",
                StatusCodes.Status400BadRequest);

        public static readonly Error OwnerNotActive =
            new("PharmacyOwner.OwnerNotActive",
                "حساب مالك الصيدلية ليس نشطاً ولا يمكن أن يكون مالكاً لصيدلية.",
                StatusCodes.Status400BadRequest);

        public static readonly Error PharmacyNotEligible =
            new("PharmacyOwner.PharmacyNotEligible",
                "تعذّر تعيين مالك لأن حالة الصيدلية المستهدفة محذوفة أو مرفوضة.",
                StatusCodes.Status400BadRequest);
    }
}
