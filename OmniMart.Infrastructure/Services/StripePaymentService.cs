using Microsoft.Extensions.Options;
using OmniMart.Application.Interfaces;
using OmniMart.Infrastructure.Services.Settings;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;

    public StripePaymentService(IOptions<StripeSettings> options)
    {
        _settings = options.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(string orderId, decimal totalAmount, string customerEmail)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            CustomerEmail = customerEmail,

            // Crucial: Maps the Stripe checkout session to our internal PaymentGroupId for Webhook processing
            ClientReferenceId = orderId,

            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        // Stripe expects the amount in the smallest currency unit (cents)
                        UnitAmount = (long)(totalAmount * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Order #{orderId} - OmniMart",
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",

            SuccessUrl = _settings.SuccessUrl,
            CancelUrl = _settings.CancelUrl,
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        return session.Url;
    }
}

