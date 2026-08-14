namespace Application.Errors;

using Application.Common;
using Microsoft.AspNetCore.Http;

public static class OrderSplittingErrors
{
    public static readonly Error OrderNotFound =
        new("OrderSplitting.OrderNotFound", "تعذّر العثور على الطلب.", StatusCodes.Status404NotFound);

    public static readonly Error NoGeoLocation =
        new("OrderSplitting.NoGeoLocation",
            "عنوان التوصيل لا يحتوي على موقع جغرافي محدد. لا يمكن إيجاد الفروع القريبة.", StatusCodes.Status400BadRequest);

    public static readonly Error NoEligibleBranches =
        new("OrderSplitting.NoEligibleBranches",
            "لا توجد فروع قريبة تدعم وضع التسليم المطلوب.", StatusCodes.Status422UnprocessableEntity);

    public static readonly Error NotEligibleForSplit =
        new("OrderSplitting.NotEligibleForSplit",
            "الطلب ليس في حالة تسمح بالتقسيم.", StatusCodes.Status400BadRequest);

    public static readonly Error NotEligibleForResplit =
        new("OrderSplitting.NotEligibleForResplit",
            "يمكن إعادة تقسيم الطلبات المعلقة أو قيد التنفيذ فقط.", StatusCodes.Status400BadRequest);

    public static readonly Error TransactionFailed =
        new("OrderSplitting.TransactionFailed",
            "حدث خطأ أثناء إتمام التقسيم. لم يتم حفظ أي تغييرات.", StatusCodes.Status500InternalServerError);
}
