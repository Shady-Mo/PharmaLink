namespace Application.Errors
{
    public static class PharmacyBranchErrors
    {
        public static readonly Error BranchNotFound = new(
            "PharmacyBranch.NotFound",
            "لا يوجد فرع مرتبط بهذا المعرّف ضمن صيدليتك.",
            StatusCodes.Status404NotFound);

        public static readonly Error PharmacyContextMissing = new(
            "PharmacyBranch.PharmacyContextMissing",
            "حساب المستخدم الحالي غير مسجل ضمن أي صيدلية.",
            StatusCodes.Status403Forbidden);

        public static readonly Error DuplicateBranchName = new(
            "PharmacyBranch.DuplicateName",
            "اسم الفرع هذا مستخدم بالفعل في صيدليتك.",
            StatusCodes.Status400BadRequest);

        public static readonly Error InvalidCoordinates = new(
            "PharmacyBranch.InvalidCoordinates",
            "يلزم توفير إحداثيات خط الطول وخط العرض معاً وضمن الحدود المسموح بها.",
            StatusCodes.Status400BadRequest);
    }
}
