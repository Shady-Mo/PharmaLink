using Microsoft.AspNetCore.Http;

namespace Application.Errors;

public static class MedicalInquiryErrors
{
    public static readonly Error NotFound =
        new("MedicalInquiry.NotFound", "Medical inquiry was not found.", StatusCodes.Status404NotFound);

    public static readonly Error AlreadyAnswered =
        new("MedicalInquiry.AlreadyAnswered", "Medical inquiry has already been answered.", StatusCodes.Status409Conflict);

    public static readonly Error EmptyQuestion =
        new("MedicalInquiry.EmptyQuestion", "Question is required.", StatusCodes.Status400BadRequest);

    public static readonly Error EmptyAnswer =
        new("MedicalInquiry.EmptyAnswer", "Answer is required.", StatusCodes.Status400BadRequest);
}
