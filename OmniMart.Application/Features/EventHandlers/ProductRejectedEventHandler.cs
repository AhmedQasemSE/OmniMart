using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.EventHandlers;

public record ProductRejectedDTO(string ProductName, string VendorEmail);

public interface IProductRejectedQueries
{
    Task<ProductRejectedDTO?> GetProductAndVendorDetailsAsync(Guid productId, Guid vendorId);
}

public class ProductRejectedEventHandler : INotificationHandler<ProductRejectedEvent>
{
    private readonly IProductRejectedQueries _queries;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductRejectedEventHandler> _logger;

    public ProductRejectedEventHandler(
        IProductRejectedQueries queries,
        IEmailService emailService,
        ILogger<ProductRejectedEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ProductRejectedEvent notification, CancellationToken cancellationToken)
    {
        var details = await _queries.GetProductAndVendorDetailsAsync(notification.ProductId, notification.VendorId);

        if (details == null || string.IsNullOrWhiteSpace(details.VendorEmail))
        {
            _logger.LogWarning("Details not found for ProductId: {ProductId} or VendorId: {VendorId}",
                notification.ProductId, notification.VendorId);
            return;
        }

        string subject = "OmniMart: Product Review Update - Action Required";
        string body = $"Hello,\n\n" +
                      $"Thank you for submitting your product '{details.ProductName}' for review.\n" +
                      $"Unfortunately, we are unable to approve it at this time for the following reason:\n\n" +
                      $"\"{notification.Reason}\"\n\n" +
                      $"Please update the product details according to our guidelines and submit it again for review.\n\n" +
                      $"Best Regards,\nOmniMart Team";

        await _emailService.SendEmailAsync(details.VendorEmail, subject, body);

        _logger.LogInformation("Rejection email sent to vendor for product: {ProductName}", details.ProductName);
    }
}