namespace Application.Errors;

public static class PharmacistErrors
{
    public static readonly Error PharmacistNotFound =
        new("Pharmacist.NotFound",
            "لا يوجد حساب صيدلي مرتبط بهذا المعرّف.",
            StatusCodes.Status404NotFound);

    public static readonly Error EmailAlreadyExists =
        new("Pharmacist.EmailAlreadyExists",
            "يوجد بالفعل حساب صيدلي مسجل بهذا البريد الإلكتروني.",
            StatusCodes.Status409Conflict);

    public static readonly Error PhoneAlreadyExists =
        new("Pharmacist.PhoneAlreadyExists",
            "يوجد بالفعل حساب صيدلي مسجل بهذا الرقم.",
            StatusCodes.Status409Conflict);

    public static readonly Error RegistrationFailed =
        new("Pharmacist.RegistrationFailed",
            "فشل في إنشاء حساب الصيدلي بسبب خطأ في الخادم. يُرجى المحاولة مرة أخرى.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error AlreadyAssigned =
        new("Pharmacist.AlreadyAssigned",
            "هذا الصيدلي معين بالفعل في صيدلية أخرى. يرجى استخدام خيار (إعادة التعيين) لتغيير صيدليته.",
            StatusCodes.Status409Conflict);

    public static readonly Error AlreadyAssignedToBranch =
        new("Pharmacist.AlreadyAssignedToBranch",
            "الصيدلي مسند بالفعل إلى هذا الفرع.",
            StatusCodes.Status409Conflict);

    public static readonly Error NoActiveAssignment =
        new("Pharmacist.NoActiveAssignment",
            "هذا الصيدلي ليس لديه تعيين نشط لإنهاء خدمته أو إلغائه.",
            StatusCodes.Status404NotFound);

    public static readonly Error PharmacyNotFound =
        new("Pharmacist.PharmacyNotFound",
            "الصيدلية المستهدفة غير موجودة.",
            StatusCodes.Status404NotFound);

    public static readonly Error BranchNotFound =
        new("Pharmacist.BranchNotFound",
            "الفرع المحدد غير موجود أو لا ينتمي إلى هذه الصيدلية.",
            StatusCodes.Status404NotFound);

    public static readonly Error AdminNotFound =
        new("Pharmacist.AdminNotFound",
            "تعذّر العثور على بيانات حساب المشرف الحالي.",
            StatusCodes.Status401Unauthorized);

    public static readonly Error AdminNotAssignedToPharmacy =
        new("Pharmacist.AdminNotAssigned",
            "حساب المشرف الحالي غير مرتبط بأي صيدلية.",
            StatusCodes.Status403Forbidden);
}
