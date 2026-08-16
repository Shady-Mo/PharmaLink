using Stripe;
using Stripe.Checkout;

namespace API.Controllers;

public class PaymentsController(
    IConfiguration configuration,
    AppDbContext context,
    ILogger<PaymentsController> logger)
    : BaseApiController
{
    private readonly string _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var signatureHeader = Request.Headers["Stripe-Signature"];
            var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _webhookSecret);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null)
                {
                    var orderIdStr = session.ClientReferenceId;
                    if (Guid.TryParse(orderIdStr, out var orderId))
                    {
                        var order = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                        if (order != null)
                        {
                            order.PaymentStatus = PaymentStatus.Paid;
                            // If you want to automatically start processing after payment:
                            if (order.OrderStatus == OrderStatus.Pending)
                            {
                                order.OrderStatus = OrderStatus.Processing;
                            }

                            var transaction = new PaymentTransaction
                            {
                                OrderId = order.OrderId,
                                StripeSessionId = session.Id,
                                StripePaymentIntentId = session.PaymentIntentId,
                                Amount = (decimal)(session.AmountTotal ?? 0) / 100m,
                                Currency = session.Currency ?? "egp",
                                Status = "Succeeded",
                                EventType = stripeEvent.Type,
                                RawData = json
                            };
                            context.PaymentTransactions.Add(transaction);

                            await context.SaveChangesAsync();
                            logger.LogInformation($"Order {orderId} payment succeeded via Stripe.");
                        }
                    }
                }
            }
            else if (stripeEvent.Type == EventTypes.CheckoutSessionExpired || stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentFailed)
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && Guid.TryParse(session.ClientReferenceId, out var orderId))
                {
                    var transaction = new PaymentTransaction
                    {
                        OrderId = orderId,
                        StripeSessionId = session.Id,
                        StripePaymentIntentId = session.PaymentIntentId,
                        Amount = (decimal)(session.AmountTotal ?? 0) / 100m,
                        Currency = session.Currency ?? "egp",
                        Status = "Failed",
                        EventType = stripeEvent.Type,
                        RawData = json
                    };
                    context.PaymentTransactions.Add(transaction);
                    await context.SaveChangesAsync();
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            logger.LogError(e, "Stripe webhook failed.");
            return BadRequest();
        }
    }
}