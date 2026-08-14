namespace Application.Errors;

public static class InventoryErrors
{
    public static readonly Error InventoryNotFound = new(
        "Inventory.NotFound",
        "تعذّر العثور على سجل المخزون المحدد.",
        StatusCodes.Status404NotFound);

    public static readonly Error InvalidQuantityV2 = new(
        "Inventory.InvalidQuantity",
        "يجب أن تكون الكمية أكبر من 0",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidQuantityV3 = new(
        "Inventory.InvalidQuantity",
        "يجب أن تكون الكمية أقل من الكمية المتاحة (كمية المخزون + الكمية المحجوزة).",
        StatusCodes.Status400BadRequest);

    public static readonly Error InsufficientStock = new(
        "Inventory.InsufficientStock",
        "الكمية المتاحة غير كافية للطلب.",
        StatusCodes.Status409Conflict);

    public static readonly Error ReleaseExceedsReserved = new(
        "Inventory.ReleaseExceedsReserved",
        "الكمية المراد إلغاء حجزها تتجاوز الكمية المحجوزة حالياً.",
        StatusCodes.Status409Conflict);

    public static readonly Error ConcurrencyConflict = new(
        "Inventory.ConcurrencyConflict",
        "حدث تعارض في التزامن أثناء تحديث المخزون. يُرجى إعادة المحاولة.",
        StatusCodes.Status409Conflict);

    public static readonly Error InvalidQuantity = new(
        "Inventory.InvalidQuantity",
        "يجب أن تكون الكمية أكبر من 0.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidIdentifier = new(
        "Inventory.InvalidIdentifier",
        "يجب أن يكون معرّف الفرع ومعرّف الدواء قيماً صحيحة وغير فارغة.",
        StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyExist = new(
        "Inventory.AlreadyExist",
        "هذا الدواء موجود بالفعل",
        StatusCodes.Status409Conflict);

    public static readonly Error DifferentBranch = new(
        "Inventory.DifferentBranch",
        "عذراً، لا تملك صلاحية إدارة المخزون الخاص بهذا الفرع.",
        StatusCodes.Status403Forbidden);

    public static readonly Error StockLowerThanReserved = new(
        "Inventory.StockLowerThanReserved",
        "لا يمكن تحديد كمية المخزون بقيمة أقل من الكمية المحجوزة حالياً.",
        StatusCodes.Status400BadRequest);

    public static readonly Error DrugNotFound = new(
        "Inventory.DrugNotFound",
        "الدواء المحدد غير موجود في الكتالوج.",
        StatusCodes.Status404NotFound);

    public static readonly Error HasReservedStock = new(
        "Inventory.HasReservedStock",
        "لا يمكن حذف هذا الصنف من المخزون لوجود كميات محجوزة منه مرتبطة بطلبات قيد الانتظار.",
        StatusCodes.Status409Conflict);
}


