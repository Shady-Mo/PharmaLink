namespace Application.Errors
{
    public static class PharmacyOrderErrors
    {
        public static readonly Error PharmacyContextMissing = new(
            "PharmacyOrder.PharmacyContextMissing",
            "حساب المستخدم الحالي غير مسجل ضمن أي صيدلية.",
            StatusCodes.Status403Forbidden);

        public static readonly Error BranchContextMissing = new(
            "PharmacyOrder.BranchContextMissing",
            "حساب المستخدم الحالي غير مسجل ضمن أي فرع.",
            StatusCodes.Status403Forbidden);

        public static readonly Error OrderNotFound = new(
            "PharmacyOrder.NotFound",
            "لا يوجد طلب مرتبط بهذا المعرّف ضمن صيدليتك.",
            StatusCodes.Status404NotFound);
    }
}
