using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class DeliveryDriverErrors
    {
        public static readonly Error DeliveryNotFound = new(
        "Delivery.NotFound",
        "Delivery was not found.",
        StatusCodes.Status404NotFound);

        public static readonly Error DeliveryPicked = new(
        "Delivery.DeliveryPicked",
        "Delivery was picked by another driver.",
        StatusCodes.Status404NotFound);
    }
}
