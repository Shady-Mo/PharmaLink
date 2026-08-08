namespace Application.Errors;

public static class OrderErrors
{
    public static readonly Error OrderMustContainItems =
        new("Order.MustContainItems",
            "Order must contain at least one item.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidDeliveryAddress =
        new("Order.InvalidDeliveryAddress",
            "DeliveryAddressID must belong to the requesting Patient.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidDrugIds =
        new("Order.InvalidDrugIds",
            "One or more invalid DrugID(s) provided.",
            StatusCodes.Status400BadRequest);

    public static readonly Error OrderNotFound =
        new("Order.NotFound",
            "Order not found or does not belong to this patient.",
            StatusCodes.Status404NotFound);

    public static readonly Error UnauthorizedOrderAccess =
        new("Order.UnauthorizedAccess",
            "You do not have permission to access this order.",
            StatusCodes.Status403Forbidden);

    public static readonly Error OrderNotEligibleForResplit =
        new("Order.NotEligibleForResplit",
            "Only Pending or Processing orders can be re-split.",
            StatusCodes.Status400BadRequest);

    public static readonly Error OrderDeliveryAddressHasNoLocation =
        new("Order.DeliveryAddressHasNoLocation",
            "The delivery address does not have a geo-location set. Cannot find nearby branches.",
            StatusCodes.Status400BadRequest);

    public static Error CreateInvalidDrugIdsError(IEnumerable<Guid> invalidIds) =>
        new("Order.InvalidDrugIds",
            $"Invalid DrugID(s): {string.Join(", ", invalidIds)}",
            StatusCodes.Status400BadRequest);

    public static readonly Error PrescriptionRequired =
        new("Order.PrescriptionRequired",
            "This order contains products that require a valid prescription.",
            StatusCodes.Status422UnprocessableEntity);

    public static readonly Error OrderCannotBeModified =
        new("Order.CannotBeModified",
            "This order cannot be modified at this stage.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidPrescription =
        new("Order.InvalidPrescription",
            "The provided prescription is invalid, expired, or already used.",
            StatusCodes.Status400BadRequest);
}

