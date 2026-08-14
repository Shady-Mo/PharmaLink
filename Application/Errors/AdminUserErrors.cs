using Application.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Errors;

public static class AdminUserErrors
{
    public static readonly Error CannotDeactivateSelf =
        new("AdminUser.CannotDeactivateSelf",
            "لا يمكنك إلغاء تنشيط حسابك الشخصي.",
            StatusCodes.Status400BadRequest);

    public static readonly Error NotFound =
        new("AdminUser.NotFound",
            "المستخدم غير موجود.",
            StatusCodes.Status404NotFound);

    public static readonly Error UpdateFailed =
        new("AdminUser.UpdateFailed",
            "فشل في تحديث حالة المستخدم. يرجى المحاولة مرة أخرى لاحقًا.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error CannotChangeOwnRole =
        new("AdminUser.CannotChangeOwnRole",
            "لا يمكنك تغيير دورك الشخصي.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidRole =
        new("AdminUser.InvalidRole",
            "الدور المحدد غير مدعوم.",
            StatusCodes.Status400BadRequest);

    public static readonly Error RoleUpdateFailed =
        new("AdminUser.RoleUpdateFailed",
            "فشل في تحديث دور المستخدم. يرجى المحاولة مرة أخرى لاحقًا.",
            StatusCodes.Status500InternalServerError);
}
