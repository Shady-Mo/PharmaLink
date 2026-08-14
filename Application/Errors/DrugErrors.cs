namespace Application.Errors;

public static class DrugErrors
{
    public static readonly Error DrugNotFound =
        new("Drug.NotFound", "تعذّر العثور على الدواء المحدد.", StatusCodes.Status404NotFound);
}