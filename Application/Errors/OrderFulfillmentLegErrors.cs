namespace Application.Errors;

public static class OrderFulfillmentLegErrors
{
    public static readonly Error NotFound = new(
        "OrderFulfillmentLeg.NotFound",
        "تعذّر العثور على جزء الطلب المراد تنفيذه.",
        StatusCodes.Status404NotFound);

    public static readonly Error Forbidden = new(
        "OrderFulfillmentLeg.Forbidden",
        "عذراً، لا تملك صلاحية الوصول إلى هذا الجزء من الطلب.",
        StatusCodes.Status403Forbidden);

    public static readonly Error InvalidTransition = new(
        "OrderFulfillmentLeg.InvalidTransition",
        "لا يمكن الانتقال إلى الحالة المطلوبة من الحالة الحالية.",
        StatusCodes.Status400BadRequest);

    public static readonly Error OverrideReasonRequired = new(
        "OrderFulfillmentLeg.OverrideReasonRequired",
        "يجب تقديم سبب عند تجاوز مسؤول النظام لحالة جزء الطلب.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidUserContext = new(
        "OrderFulfillmentLeg.InvalidUserContext",
        "بيانات جلسة تسجيل الدخول غير مكتملة. يُرجى تسجيل الدخول مرة أخرى.",
        StatusCodes.Status401Unauthorized);
}
