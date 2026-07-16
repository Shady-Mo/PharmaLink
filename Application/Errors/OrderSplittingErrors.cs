namespace Application.Errors;

using Application.Common;
using Microsoft.AspNetCore.Http;

public static class OrderSplittingErrors
{
    public static readonly Error OrderNotFound =
        new("OrderSplitting.OrderNotFound", "Order not found.", StatusCodes.Status404NotFound);

    public static readonly Error NoGeoLocation =
        new("OrderSplitting.NoGeoLocation",
            "The delivery address has no geo-location. Cannot find nearby branches.", StatusCodes.Status400BadRequest);

    public static readonly Error NoEligibleBranches =
        new("OrderSplitting.NoEligibleBranches",
            "No nearby branches support the requested fulfillment mode.", StatusCodes.Status422UnprocessableEntity);

    public static readonly Error NotEligibleForSplit =
        new("OrderSplitting.NotEligibleForSplit",
            "Order is not in a state that allows splitting.", StatusCodes.Status400BadRequest);

    public static readonly Error NotEligibleForResplit =
        new("OrderSplitting.NotEligibleForResplit",
            "Only Pending or Processing orders can be re-split.", StatusCodes.Status400BadRequest);

    public static readonly Error TransactionFailed =
        new("OrderSplitting.TransactionFailed",
            "An error occurred while committing the split. No changes were saved.", StatusCodes.Status500InternalServerError);
}
