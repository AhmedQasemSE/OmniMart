using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Orders.Commands;
using Stripe;
using StripeEvents = Stripe.Events;

namespace OmniMart.Controllers;

[Route("api/webhook")]
[ApiController]
public class WebhookController:BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhookController> _logger;
    private readonly string _webhookSecret;

    public WebhookController(IMediator mediator, ILogger<WebhookController> logger, IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _webhookSecret = configuration["StripeSettings:WebhookSecret"] ?? string.Empty;
    }
    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _webhookSecret
            );

            if (stripeEvent.Type == Stripe.EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                if (session != null)
                {
                    var orderIdString = session.ClientReferenceId;

                    if (Guid.TryParse(orderIdString, out Guid paymentGroupId))
                    {
                        var command = new CompletePaymentCommand(paymentGroupId, session.Id);
                        await _mediator.Send(command, cancellationToken);
                    }
                }
            }

            return Ok(); 
            
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe Webhook Error: Invalid Signature!");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe Webhook.");
            return StatusCode(500);
        }
    }
}
    

