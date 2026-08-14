namespace Application.Errors;

public static class PatientPharmacyErrors
{
    public static readonly Error InvalidCoordinates = new(
        "PatientPharmacy.InvalidCoordinates",
        "يجب أن تقع قيمة خط العرض بين -90 و90، وخط الطول بين -180 و180.",
        StatusCodes.Status400BadRequest);

    public static readonly Error NoPharmaciesFound = new(
        "PatientPharmacy.NoPharmaciesFound",
        "لم يتم العثور على صيدليات ضمن النطاق المحدد.",
        StatusCodes.Status404NotFound);
}
