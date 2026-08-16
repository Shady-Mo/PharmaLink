using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly string _secretKey;

    public StripePaymentService(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"] ?? "";
        StripeConfiguration.ApiKey = _secretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl)
    {
        var lineItems = new List<SessionLineItemOptions>();

        if (order.Items != null && order.Items.Any() && order.Items.All(i => i.Drug != null))
        {
            foreach (var item in order.Items)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Drug.Price * 100), // Stripe expects amount in cents
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Drug.BrandName ?? "Medicine",
                            Description = item.Drug.ArabicName
                        },
                    },
                    Quantity = item.QuantityNeeded,
                });
            }
        }
        else
        {
            // Fallback if items are not loaded (e.g., just passing total amount)
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(order.TotalAmount * 100),
                    Currency = "egp",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Pharmacy Order",
                        Description = $"Order #{order.OrderId}"
                    },
                },
                Quantity = 1,
            });
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            ClientReferenceId = order.OrderId.ToString(),
            CustomerEmail = order.Patient?.Email
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        order.StripeSessionId = session.Id;
        order.StripePaymentIntentId = session.PaymentIntentId;

        return session.Url;
    }
}
