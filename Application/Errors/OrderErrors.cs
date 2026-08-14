namespace Application.Errors;

public static class OrderErrors
{
    public static readonly Error OrderMustContainItems =
        new("Order.MustContainItems",
            "يجب أن يحتوي الطلب على صنف واحد على الأقل.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidDeliveryAddress =
        new("Order.InvalidDeliveryAddress",
            "عنوان التوصيل المحدد لا يخص المريض الحالي.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidDrugIds =
        new("Order.InvalidDrugIds",
            "أحد معرّفات الأدوية المدخلة (أو أكثر) غير صحيح.",
            StatusCodes.Status400BadRequest);

    public static readonly Error OrderNotFound =
        new("Order.NotFound",
            "تعذّر العثور على الطلب أو أنه لا ينتمي إلى هذا المريض.",
            StatusCodes.Status404NotFound);

    public static readonly Error UnauthorizedOrderAccess =
        new("Order.UnauthorizedAccess",
            "عذراً، لا تملك صلاحية عرض هذا الطلب.",
            StatusCodes.Status403Forbidden);

    public static readonly Error OrderNotEligibleForResplit =
        new("Order.NotEligibleForResplit",
            "يُسمح بإعادة تقسيم الطلبات المعلقة أو قيد التنفيذ فقط.",
            StatusCodes.Status400BadRequest);

    public static readonly Error OrderDeliveryAddressHasNoLocation =
        new("Order.DeliveryAddressHasNoLocation",
            "عنوان التوصيل لا يحتوي على موقع جغرافي محدد. لا يمكن إيجاد الفروع القريبة.",
            StatusCodes.Status400BadRequest);

    public static Error CreateInvalidDrugIdsError(IEnumerable<Guid> invalidIds) =>
        new("Order.InvalidDrugIds",
            $"معرّف الدواء (أو المعرّفات) غير صالح: {string.Join(", ", invalidIds)}",
            StatusCodes.Status400BadRequest);

    public static readonly Error PrescriptionRequired =
        new("Order.PrescriptionRequired",
            "يحتوي هذا الطلب على منتجات تتطلب وصفة طبية صالحة.",
            StatusCodes.Status422UnprocessableEntity);

    public static readonly Error OrderCannotBeModified =
        new("Order.CannotBeModified",
            "لا يمكن تعديل هذا الطلب في هذه المرحلة.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidPrescription =
        new("Order.InvalidPrescription",
            "الوصفة المقدمة غير صالحة أو منتهية الصلاحية أو تم استخدامها بالفعل.",
            StatusCodes.Status400BadRequest);
}

