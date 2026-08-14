using Microsoft.AspNetCore.Http;

namespace Application.Errors;

public static class PrescriptionReviewErrors
{
    public static readonly Error NotFound =
        new("PrescriptionReview.NotFound",
            "تعذّر العثور على مراجعة الروشتة.",
            StatusCodes.Status404NotFound);

    public static readonly Error AlreadyReviewed =
        new("PrescriptionReview.AlreadyReviewed",
            "تمت مراجعة هذه الروشتة بالفعل.",
            StatusCodes.Status409Conflict);

    public static readonly Error NotApproved =
        new("PrescriptionReview.NotApproved",
            "يجب اعتماد الوصفة الطبية أولاً لتتمكن من إنشاء الطلب.",
            StatusCodes.Status400BadRequest);

    public static readonly Error OrderAlreadyCreated =
        new("PrescriptionReview.OrderAlreadyCreated",
            "تم إنشاء طلب بالفعل من هذه الروشتة.",
            StatusCodes.Status409Conflict);

    public static readonly Error AIExtractionFailed =
        new("PrescriptionReview.AIExtractionFailed",
            "تعذّرت قراءة البيانات عبر الذكاء الاصطناعي. الرجاء إعادة المحاولة لاحقاً.",
            StatusCodes.Status502BadGateway);

    public static readonly Error AIReturnedNoMedicines =
        new("PrescriptionReview.AIReturnedNoMedicines",
            "لم يتمكن الذكاء الاصطناعي من التعرف على أي أدوية في الصورة المرفوعة. يُرجى رفع صورة أوضح للروشتة.",
            StatusCodes.Status422UnprocessableEntity);

    public static Error InvalidPrescription(string message) =>
        new("PrescriptionReview.InvalidPrescription",
            message,
            StatusCodes.Status422UnprocessableEntity);

    public static readonly Error MedicineNotFound =
        new("PrescriptionReview.MedicineNotFound",
            "لم يتم العثور على واحد أو أكثر من الأدوية المطلوبة في نتيجة المراجعة.",
            StatusCodes.Status404NotFound);

    public static readonly Error MedicineCannotBeAddedToCart =
        new("PrescriptionReview.MedicineCannotBeAddedToCart",
            "تعذّرت إضافة دواء أو أكثر إلى السلة إما لعدم توفره، أو لعدم العثور عليه، أو لأنه ليس بديلاً معتمداً.",
            StatusCodes.Status422UnprocessableEntity);

    public static readonly Error Forbidden =
        new("PrescriptionReview.Forbidden",
            "ليس لديك إذن بالوصول إلى مراجعة هذه الروشتة.",
            StatusCodes.Status403Forbidden);
}
