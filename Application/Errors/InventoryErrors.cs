namespace Application.Errors;

public static class InventoryErrors
{
    public static readonly Error InventoryNotFound = new(
        "Inventory.NotFound",
        "The specified inventory record was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error InsufficientStock = new(
        "Inventory.InsufficientStock",
        "Available stock is insufficient for the requested quantity.",
        StatusCodes.Status409Conflict);

    public static readonly Error ReleaseExceedsReserved = new(
        "Inventory.ReleaseExceedsReserved",
        "The quantity to release exceeds the currently reserved quantity.",
        StatusCodes.Status409Conflict);

    public static readonly Error ConcurrencyConflict = new(
        "Inventory.ConcurrencyConflict",
        "A concurrency conflict occurred while updating the inventory. Please retry the operation.",
        StatusCodes.Status409Conflict);

    public static readonly Error InvalidQuantity = new(
        "Inventory.InvalidQuantity",
        "Quantity must be greater than zero.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidIdentifier = new(
        "Inventory.InvalidIdentifier",
        "Branch identifier and drug identifier must be valid non-empty values.",
        StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyExist = new(
        "Inventory.AlreadyExist",
        "This drug already exist",
        StatusCodes.Status409Conflict
        );

    public static readonly Error DifferentBranch = new(
        "Inventory.DifferentBranch",
        "You do not have permission to manage inventory for this branch",
        StatusCodes.Status403Forbidden
        );

    public static readonly Error StockLowerThanReserved = new(
    "Inventory.StockLowerThanReserved",
    "Cannot set stock quantity lower than currently reserved quantity.",
    StatusCodes.Status400BadRequest
    );

}