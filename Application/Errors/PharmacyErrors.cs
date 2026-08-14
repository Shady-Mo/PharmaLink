namespace Application.Errors
{
    public static class PharmacyErrors
    {
        public static readonly Error PharmacyNotFound =
        new("Pharmacy.PharmacyNotFound",
            "لا توجد صيدلية مرتبطة بهذا المعرّف.",
            StatusCodes.Status404NotFound);

        public static readonly Error Forbidden = new(
            "Pharmacy.Forbidden",
            "لا تملك صلاحية الدخول لهذه الصيدلية.",
            StatusCodes.Status403Forbidden);

        public static readonly Error LicenseNumberNotUnique = new(
            "Pharmacy.LicenseNumberNotUnique",
            "رقم الترخيص هذا مستخدم بالفعل لصيدلية أخرى.",
            StatusCodes.Status400BadRequest);

        public static readonly Error InvalidOwnerUserId =
        new("Pharmacy.InvalidOwnerUserId",
            "معرّف المالك غير صالح.",
            StatusCodes.Status400BadRequest);

        public static readonly Error PharmacyNotEligible =
        new("Pharmacy.NotEligible",
            "لا يمكن تعيين مالك لصيدلية محذوفة أو مرفوضة.",
            StatusCodes.Status400BadRequest);

        public static readonly Error InvalidLogoType = new(
            "Pharmacy.InvalidLogoType",
            "يجب أن يكون الشعار ملف صورة بصيغة (.jpg أو .jpeg أو .png أو .webp).",
            StatusCodes.Status400BadRequest);

        public static readonly Error LogoFileTooLarge = new(
            "Pharmacy.LogoFileTooLarge",
            "يجب ألا يتجاوز حجم ملف الشعار 2 ميجابايت.",
            StatusCodes.Status400BadRequest);
    }
}
