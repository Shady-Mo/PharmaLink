namespace Application.Errors
{

    public static class AddressErrors
    {
        public static readonly Error NotFound =
            new("Address.NotFound", "العنوان غير موجود.", StatusCodes.Status404NotFound);

        public static readonly Error Forbidden =
            new("Address.Forbidden",
                "عذراً، لا تملك الصلاحية للوصول إلى هذا العنوان.",
                StatusCodes.Status403Forbidden);

        public static readonly Error AuditReasonRequired =
            new("Address.AuditReasonRequired",
                "يُلزم مسؤول النظام بتقديم سبب عند الوصول إلى عنوان المريض.",
                StatusCodes.Status400BadRequest);
        public static readonly Error InUse =
          new("Address.InUse",
              "هذا العنوان مرتبط بطلبات حالية ولا يمكن حذفه.",
              StatusCodes.Status409Conflict);
        public static readonly Error AddressAlreadyDefault =
            new("AddressAlreadyDefault",
                "هذا العنوان هو العنوان الافتراضي بالفعل."
                , StatusCodes.Status409Conflict);

        public static readonly Error AddressIsTheDefault =
            new("AddressIsTheDefault",
                "هذا هو العنوان الافتراضي، لا يمكنك حذفه أو إلغاء تعيينه. يرجى تعيين عنوان آخر كافتراضي أولاً."
                , StatusCodes.Status409Conflict);

    }
}
