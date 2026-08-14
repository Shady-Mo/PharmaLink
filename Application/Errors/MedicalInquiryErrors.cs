using Microsoft.AspNetCore.Http;

namespace Application.Errors;

public static class MedicalInquiryErrors
{
    public static readonly Error NotFound =
        new("MedicalInquiry.NotFound", "تعذّر العثور على الاستفسار الطبي.", StatusCodes.Status404NotFound);

    public static readonly Error AlreadyAnswered =
        new("MedicalInquiry.AlreadyAnswered", "تم الرد على الاستفسار الطبي بالفعل.", StatusCodes.Status409Conflict);

    public static readonly Error EmptyQuestion =
        new("MedicalInquiry.EmptyQuestion", "السؤال مطلوب.", StatusCodes.Status400BadRequest);

    public static readonly Error EmptyAnswer =
        new("MedicalInquiry.EmptyAnswer", "الإجابة مطلوبة.", StatusCodes.Status400BadRequest);

    public static readonly Error CannotClose =
        new("MedicalInquiry.CannotClose", "يمكن إغلاق الاستفسارات التي تم الرد عليها فقط.", StatusCodes.Status409Conflict);
}
