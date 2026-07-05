namespace Domain.Enums;

public enum UserStatus : byte {
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public enum VerificationStatus : byte {
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

public enum FulfillmentMode : byte {
    Delivery = 1,
    Pickup = 2
}

public enum OrderStatus : byte {
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum ItemStatus : byte {
    Pending = 1,
    Fulfilled = 2,
    Cancelled = 3
}

public enum LegType : byte {
    Preparation = 1,
    Delivery = 2
}

public enum LegStatus : byte {
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
