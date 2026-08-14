using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Errors
{
    public static class DeliveryDriverErrors
    {
        public static readonly Error DeliveryNotFound = new(
        "Delivery.NotFound",
        "لم يتم العثور على التوصيل.",
        StatusCodes.Status404NotFound);

        public static readonly Error DeliveryPicked = new(
        "Delivery.DeliveryPicked",
        "تم اختيار التوصيل بواسطة سائق آخر.",
        StatusCodes.Status404NotFound);
    }
}
