namespace Domain.Enums;

public enum UserStatus : byte
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public enum VerificationStatus : byte
{
    Pending = 1,
    Verified = 2,
    Rejected = 3,
    Deleted = 4
}

public enum FulfillmentMode : byte
{
    Delivery = 1,
    Pickup = 2
}

public enum OrderStatus : byte
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Completed = 4,
    Cancelled = 5
}

public enum ItemStatus : byte
{
    Pending = 1,
    Fulfilled = 2,
    Cancelled = 3,
    Awarded = 4,
    Unavailable = 5
}

public enum LegType : byte
{
    Preparation = 1,
    Delivery = 2
}

public enum LegStatus : byte
{
    Assigned = 1,
    Preparing = 2,
    ReadyForPickup = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Cancelled = 6
}

public enum PrescriptionReviewStatus : byte
{
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    OrderCreated = 4
}



public enum DrugAvailabilityStatus : byte
{
    OutOfStock = 1,
    LowStock = 2,
    InStock = 3
}

public enum InventoryStockStatus : byte
{
    OutOfStock = 1,
    LowStock = 2,
    Available = 3
}

public enum InventoryStatusFilter : byte
{
    All = 0,
    Available = 1,
    LowStock = 2,
    OutOfStock = 3
}

public enum PharmacyOrderSort : byte
{
    NewestFirst = 0,

    OldestFirst = 1,

    HighestAmount = 2,

    LowestAmount = 3
}

public enum PrescriptionProcessingStatus : byte
{
    Unknown = 0,
    Rejected = 1,
    Completed = 2,
    NeedsPatientApproval = 3,
    PendingPharmacistReview = 4,
    Processing = 5,
    Failed = 6
}

public enum PrescriptionMedicineMatchStatus : byte
{
    NotFound = 0,
    ExactMatch = 1,
    FuzzyMatch = 2,
    AlternativeSuggested = 3,
    Unavailable = 4
}
