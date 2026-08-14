namespace Application.Errors;

public static class PatientErrors
{
    public static readonly Error PatientNotFound =
        new("Patient.NotFound",
            "تعذّر العثور على الملف الشخصي للمريض المسجل حالياً.",
            StatusCodes.Status404NotFound);

    public static readonly Error PhoneAlreadyExists =
        new("Patient.PhoneAlreadyExists",
            "رقم الهاتف هذا مستخدم بالفعل لحساب مريض آخر.",
            StatusCodes.Status409Conflict);
}