namespace Application.Errors
{

    public static class AddressErrors
    {
        public static readonly Error NotFound =
            new("Address.NotFound", "Address not found.", StatusCodes.Status404NotFound);

        public static readonly Error Forbidden =
            new("Address.Forbidden",
                "You do not have permission to access this address.",
                StatusCodes.Status403Forbidden);

        public static readonly Error AuditReasonRequired =
            new("Address.AuditReasonRequired",
                "A reason is required when a System Admin accesses a patient's address.",
                StatusCodes.Status400BadRequest);
        public static readonly Error InUse =
          new("Address.InUse",
              "This address is linked to existing orders and cannot be deleted.",
              StatusCodes.Status409Conflict);
        public static readonly Error AddressAlreadyDefault =
            new("AddressAlreadyDefault",
                "This address is already the default address."
                , StatusCodes.Status409Conflict);

        public static readonly Error AddressIsTheDefault =
            new("AddressIsTheDefault",
                "هذا هو العنوان الافتراضي، لا يمكنك حذفه أو إلغاء تعيينه. يرجى تعيين عنوان آخر كافتراضي أولاً."
                , StatusCodes.Status409Conflict);

    }
}
