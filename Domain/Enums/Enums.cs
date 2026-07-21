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
    Rejected = 3
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
}public enum DrugCategory : byte
{
    PainRelievers = 1,
    Antibiotics = 2,
    DigestiveSystem = 3,
    Diabetes = 4,
    Cardiovascular = 5,
    BloodPressure = 6,
    AntiInflammatory = 7,
    Other = 8
}

public enum DrugAvailabilityStatus : byte
{
    OutOfStock = 1,
    LowStock = 2,
    InStock = 3
}