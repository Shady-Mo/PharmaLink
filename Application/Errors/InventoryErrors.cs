namespace Application.Errors;

public static class InventoryErrors
{
    public static readonly Error InventoryNotFound = new(
        "Inventory.NotFound",
        "The specified inventory record was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error InvalidQuantityV2 = new(
        "Inventory.InvalidQuantity",
        "Quantity must be greater than 0",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidQuantityV3 = new(
        "Inventory.InvalidQuantity",
        "Quantity must be less than Avilable quantity(Stock Q + Reserved Q)",
        StatusCodes.Status400BadRequest);

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
        StatusCodes.Status409Conflict);

    public static readonly Error DifferentBranch = new(
        "Inventory.DifferentBranch",
        "You do not have permission to manage inventory for this branch",
        StatusCodes.Status403Forbidden);

    public static readonly Error StockLowerThanReserved = new(
        "Inventory.StockLowerThanReserved",
        "Cannot set stock quantity lower than currently reserved quantity.",
        StatusCodes.Status400BadRequest);

    public static readonly Error DrugNotFound = new(
        "Inventory.DrugNotFound",
        "The specified drug does not exist in the catalog.",
        StatusCodes.Status404NotFound);

    public static readonly Error HasReservedStock = new(
        "Inventory.HasReservedStock",
        "This inventory item cannot be deleted because it has reserved stock tied to pending orders.",
        StatusCodes.Status409Conflict);
}


