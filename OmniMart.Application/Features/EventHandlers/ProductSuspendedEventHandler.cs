using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.EventHandlers;

public record ProductSuspendedDTO(string ProductName, string VendorEmail);

public interface IProductSuspendedQueries
{
    Task<ProductSuspendedDTO?> GetProductAndVendorDetailsAsync(Guid productId, Guid vendorId);
}

public class ProductSuspendedEventHandler : INotificationHandler<ProductSuspendedEvent>
{
    private readonly IProductSuspendedQueries _queries;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductSuspendedEventHandler> _logger;

    public ProductSuspendedEventHandler(
        IProductSuspendedQueries queries,
        IEmailService emailService,
        ILogger<ProductSuspendedEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ProductSuspendedEvent notification, CancellationToken cancellationToken)
    {
        var details = await _queries.GetProductAndVendorDetailsAsync(notification.ProductId, notification.VendorId);

        if (details == null || string.IsNullOrWhiteSpace(details.VendorEmail))
        {
            _logger.LogWarning("Details not found for ProductId: {ProductId} or VendorId: {VendorId}",
                notification.ProductId, notification.VendorId);
            return;
        }

        string subject = "OmniMart: Urgent - Product Suspended";
        string body = $"Hello,\n\n" +
                      $"This is an urgent notification regarding your product '{details.ProductName}'.\n" +
                      $"Your product has been suspended and removed from the active catalog by our administration team.\n\n" +
                      $"Reason for suspension:\n" +
                      $"\"{notification.Reason}\"\n\n" +
                      $"If you believe this is a mistake or if you have corrected the issue, please contact support.\n\n" +
                      $"Best Regards,\nOmniMart Team";

        await _emailService.SendEmailAsync(details.VendorEmail, subject, body);

        _logger.LogInformation("Suspension email sent to vendor for product: {ProductName}", details.ProductName);
    }
}