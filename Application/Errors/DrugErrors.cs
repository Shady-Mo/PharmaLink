
namespace Application.Errors;

public static class DrugErrors
{
    public static readonly Error DrugNotFound =
        new("Drug.NotFound", "The specified drug was not found.", StatusCodes.Status404NotFound);
}