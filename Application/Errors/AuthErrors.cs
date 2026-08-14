namespace Application.Errors;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists =
        new("Auth.EmailAlreadyExists",
            "يوجد حساب مسجل بالفعل بهذا العنوان الإلكتروني.",
            StatusCodes.Status409Conflict);

    public static readonly Error PhoneAlreadyExists =
        new("Auth.PhoneAlreadyExists",
            "يوجد حساب مسجل بالفعل برقم الهاتف المحدد.",
            StatusCodes.Status409Conflict);

    public static readonly Error RegistrationFailed =
        new("Auth.RegistrationFailed",
            "فشل تسجيل الحساب بسبب خطأ في الخادم. يرجى المحاولة مرة أخرى.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", 
        "البريد الإلكتروني أو كلمة المرور غير صحيحة.", 
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidEmail = new("Auth.InvalidEmails",
        "البريد الإلكتروني غير صحيح.",
        StatusCodes.Status404NotFound);

    public static readonly Error AccountDeactivated =
    new("Auth.AccountDeactivated",
        "هذا الحساب تم إلغاء تنشيطه. يرجى التواصل مع الدعم.",
        StatusCodes.Status403Forbidden);

    public static readonly Error PhoneNotVerified =
        new("Auth.PhoneNotVerified",
            "لم يتم التحقق من رقم الهاتف الخاص بك. يرجى التحقق من رقم الهاتف قبل تسجيل الدخول.",
            StatusCodes.Status403Forbidden);

    public static readonly Error TokenError =
       new("Auth.TokenError",
           "الرمز غير صحيح.",
           StatusCodes.Status400BadRequest);

    public static readonly Error CurrentPasswordIncorrect =
        new("Auth.CurrentPasswordIncorrect",
            "كلمة المرور الحالية غير صحيحة.",
            StatusCodes.Status401Unauthorized);

    public static readonly Error PasswordChangeRequired =
        new("Auth.PasswordChangeRequired",
            "فشل في تغيير كلمة المرور بسبب خطأ في الخادم. يرجى المحاولة مرة أخرى.",
            StatusCodes.Status500InternalServerError);

    public static readonly Error UserNotFound =
        new("Auth.UserNotFound",
            "المستخدم غير موجود.",
            StatusCodes.Status404NotFound);
}
