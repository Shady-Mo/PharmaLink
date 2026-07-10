using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class FulfillmentErrors
    {
        public static readonly Error OrderNotFound = new("Fulfillment.OrderNotFound", "The requested order was not found.", StatusCodes.Status400BadRequest);
        public static readonly Error AddressNotFound = new("Fulfillment.AddressNotFound", "The delivery address for this order is missing or invalid.", StatusCodes.Status400BadRequest);
        public static readonly Error EngineFailure = new("From AdminOrderConroller", "EngineFailure", StatusCodes.Status500InternalServerError);
    }
}