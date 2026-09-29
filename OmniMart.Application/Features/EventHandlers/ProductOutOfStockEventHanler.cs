using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
namespace OmniMart.Application.Features.EventHandlers;

public record ProductOutOfStockEventDTo(string VendorEmail, string ProductName, string VariantSKU);
public interface IProductOutOfStockEventsQueries
{
    Task<ProductOutOfStockEventDTo?> GetProductOutOfStockEvent(Guid id);
}

public class ProductOutOfStockEventHandler : INotificationHandler<ProductOutOfStockEvent>
{
    private readonly IProductOutOfStockEventsQueries _queries;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductOutOfStockEventHandler> _logger;

    public ProductOutOfStockEventHandler(
        IProductOutOfStockEventsQueries queries,
        IEmailService emailService,
        ILogger<ProductOutOfStockEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _logger = logger;
    }
    public async Task Handle(ProductOutOfStockEvent notification, CancellationToken cancellationToken)
    {
        var details = await _queries.GetProductOutOfStockEvent(notification.variantId);

        if (details == null || string.IsNullOrWhiteSpace(details.VendorEmail))
        {
            _logger.LogWarning("Out of stock details not found for VariantId: {VariantId}", notification.variantId);
            return;
        }

        string subject = "⚠️ Action Required: Your Product is Out of Stock!";
        string body = $@"Hello,

          We are reaching out to let you know that one of your products has completely sold out and is currently out of stock on OmniMart.

          Product Details:
         - Product Name: {details.ProductName}
         - Variant/SKU: {details.VariantSKU}

         Products that are out of stock lose visibility in customer searches. To avoid losing potential sales, please log in to your vendor dashboard and restock this item as soon as possible.

          Best Regards,
          The OmniMart Team";

        await _emailService.SendEmailAsync(details.VendorEmail, subject, body);

        _logger.LogInformation("Out of stock email sent to vendor {Email} for SKU {SKU}", details.VendorEmail, details.VariantSKU);
    }
}
