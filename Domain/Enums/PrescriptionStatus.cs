namespace Domain.Enums;

public enum PrescriptionStatus
{
    Pending = 0,
    AttachedToOrder = 1,
    Deleted = 2,
    Expired = 3,
    PendingReview = 4,
    Approved = 5,
    Rejected = 6
}

