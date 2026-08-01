using Microsoft.AspNetCore.Http;

namespace Application.Errors;

public static class PrescriptionReviewErrors
{
    public static readonly Error NotFound =
        new("PrescriptionReview.NotFound",
            "Prescription review not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error AlreadyReviewed =
        new("PrescriptionReview.AlreadyReviewed",
            "This prescription has already been reviewed.",
            StatusCodes.Status409Conflict);

    public static readonly Error NotApproved =
        new("PrescriptionReview.NotApproved",
            "Only approved prescriptions can proceed to order creation.",
            StatusCodes.Status400BadRequest);

    public static readonly Error OrderAlreadyCreated =
        new("PrescriptionReview.OrderAlreadyCreated",
            "An order has already been created from this prescription.",
            StatusCodes.Status409Conflict);

    public static readonly Error AIExtractionFailed =
        new("PrescriptionReview.AIExtractionFailed",
            "AI extraction failed. Please try again later.",
            StatusCodes.Status502BadGateway);

    public static readonly Error AIReturnedNoMedicines =
        new("PrescriptionReview.AIReturnedNoMedicines",
            "The AI could not detect any medicines in the uploaded image. Please upload a clearer image of the prescription.",
            StatusCodes.Status422UnprocessableEntity);

    public static Error InvalidPrescription(string message) =>
        new("PrescriptionReview.InvalidPrescription",
            message,
            StatusCodes.Status422UnprocessableEntity);

    public static readonly Error MedicineNotFound =
        new("PrescriptionReview.MedicineNotFound",
            "One or more medicines in your request were not found in this review.",
            StatusCodes.Status404NotFound);

    public static readonly Error MedicineCannotBeAddedToCart =
        new("PrescriptionReview.MedicineCannotBeAddedToCart",
            "One or more selected medicines cannot be added to the cart because they are unavailable, not found, or not approved alternatives.",
            StatusCodes.Status422UnprocessableEntity);

    public static readonly Error Forbidden =
        new("PrescriptionReview.Forbidden",
            "You do not have permission to access this prescription review.",
            StatusCodes.Status403Forbidden);
}
