namespace Application.Errors;

public static class OrderFulfillmentLegErrors
{
    public static readonly Error NotFound = new(
        "OrderFulfillmentLeg.NotFound",
        "Fulfillment leg was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error Forbidden = new(
        "OrderFulfillmentLeg.Forbidden",
        "You are not allowed to access this fulfillment leg.",
        StatusCodes.Status403Forbidden);

    public static readonly Error InvalidTransition = new(
        "OrderFulfillmentLeg.InvalidTransition",
        "The requested status transition is not allowed.",
        StatusCodes.Status400BadRequest);

    public static readonly Error OverrideReasonRequired = new(
        "OrderFulfillmentLeg.OverrideReasonRequired",
        "A reason is required when an admin overrides a fulfillment leg status.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidUserContext = new(
        "OrderFulfillmentLeg.InvalidUserContext",
        "The current user token is missing required identity claims.",
        StatusCodes.Status401Unauthorized);
}
