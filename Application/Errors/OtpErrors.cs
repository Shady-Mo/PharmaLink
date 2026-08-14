namespace Application.Errors;

public static class OtpErrors
{
    public static readonly Error UserNotFound =
        new("Otp.UserNotFound",
            "لا يوجد حساب مرتبط بمعرّف المستخدم هذا.",
            StatusCodes.Status404NotFound);

    public static readonly Error PhoneAlreadyVerified =
        new("Otp.PhoneAlreadyVerified",
            "تم التحقق من رقم الهاتف هذا بالفعل.",
            StatusCodes.Status409Conflict);

    public static readonly Error RateLimitExceeded =
        new("Otp.RateLimitExceeded",
            "تجاوزت الحد المسموح به لمحاولات إدخال رمز التحقق. يُرجى الانتظار 5 دقائق قبل المحاولة مرة أخرى.",
            StatusCodes.Status429TooManyRequests);

    public static readonly Error InvalidOrExpired =
        new("Otp.InvalidOrExpired",
            "رمز التحقق غير صالح أو منتهي الصلاحية.",
            StatusCodes.Status400BadRequest);

    public static readonly Error WebhookFailed =
        new("Otp.WebhookFailed",
            "فشل في إرسال رمز التحقق. يُرجى المحاولة مرة أخرى لاحقًا.",
            StatusCodes.Status503ServiceUnavailable);
}
