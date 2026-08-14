namespace Application.Errors;

public static class DashboardErrors
{
    public static readonly Error PharmacyContextMissing = new(
        "Dashboard.PharmacyContextMissing",
        "لا توجد صيدلية مرتبطة بالمستخدم الحالي.",
        StatusCodes.Status403Forbidden);

    public static readonly Error BranchContextMissing = new(
        "Dashboard.BranchContextMissing",
        "لا يوجد فرع مرتبط بالمستخدم الحالي.",
        StatusCodes.Status403Forbidden);

    public static readonly Error PharmacyNotFound = new(
        "Dashboard.PharmacyNotFound",
        "تعذّر العثور على الصيدلية المرتبطة بالمستخدم الحالي.",
        StatusCodes.Status404NotFound);

    public static readonly Error BranchNotFound = new(
        "Dashboard.BranchNotFound",
        "تعذّر العثور على الفرع المرتبط بالمستخدم الحالي.",
        StatusCodes.Status404NotFound);

    public static readonly Error PharmacyRetrievalFailed = new(
        "Dashboard.PharmacyRetrievalFailed",
        "فشل تحميل لوحة تحكم الصيدلية.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error BranchRetrievalFailed = new(
        "Dashboard.BranchRetrievalFailed",
        "فشل تحميل لوحة تحكم الفرع.",
        StatusCodes.Status500InternalServerError);
}
