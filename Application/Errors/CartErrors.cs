namespace Application.Errors;

public static class CartErrors
{
    public static readonly Error DrugNotFound =
        new("Cart.DrugNotFound",
            "The specified drug does not exist or is inactive.",
            StatusCodes.Status404NotFound);

    public static readonly Error CartItemNotFound =
        new("Cart.ItemNotFound",
            "The cart item was not found or does not belong to this patient's cart.",
            StatusCodes.Status404NotFound);

    public static readonly Error InvalidQuantity =
        new("Cart.InvalidQuantity",
            "Quantity must be greater than 0.",
            StatusCodes.Status400BadRequest);
}
