namespace Application.Errors;

public static class CartErrors
{
    public static readonly Error DrugNotFound =
        new("Cart.DrugNotFound",
            "الدواء المحدد غير موجود أو غير نشط.",
            StatusCodes.Status404NotFound);

    public static readonly Error CartItemNotFound =
        new("Cart.ItemNotFound",
            "تعذّر العثور على العنصر أو أنه لا ينتمي إلى سلة هذا المريض.",
            StatusCodes.Status404NotFound);

    public static readonly Error CartNotFound =
        new("Cart.NotFound",
            "السلة غير موجودة.",
            StatusCodes.Status404NotFound);

    public static readonly Error InvalidQuantity =
        new("Cart.InvalidQuantity",
            "يجب أن تكون الكمية أكبر من 0.",
            StatusCodes.Status400BadRequest);
}
