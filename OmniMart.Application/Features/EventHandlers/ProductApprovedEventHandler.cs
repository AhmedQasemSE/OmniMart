using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Features.EventHandlers;
public record ProductApprovedDTO(string ProductName, string VendorEmail);

public interface IProductApprovedQueries
{
    Task<ProductApprovedDTO?> GetProductAndVendorDetailsAsync(Guid productId, Guid vendorId);
}

public class ProductApprovedEventHandler : INotificationHandler<ProductApprovedEvent>
{
    private readonly IProductApprovedQueries _queries;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductApprovedEventHandler> _logger;

    public ProductApprovedEventHandler(
        IProductApprovedQueries queries,
        IEmailService emailService,
        ILogger<ProductApprovedEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ProductApprovedEvent notification, CancellationToken cancellationToken)
    {
        var details = await _queries.GetProductAndVendorDetailsAsync(notification.ProductId, notification.VendorId);

        if (details == null || string.IsNullOrWhiteSpace(details.VendorEmail))
        {
            _logger.LogWarning("Details not found for ProductId: {ProductId} or VendorId: {VendorId}",
                notification.ProductId, notification.VendorId);
            return;
        }

        string subject = "OmniMart: Product Approved! 🎉";
        string body = $"Hello,\n\n" +
                      $"Great news! Your product '{details.ProductName}' has been approved by our team.\n" +
                      $"It is now live and visible to all customers on OmniMart.\n\n" +
                      $"We wish you great sales!\n\n" +
                      $"Best Regards,\nOmniMart Team";

        await _emailService.SendEmailAsync(details.VendorEmail, subject, body);

        _logger.LogInformation("Approval email sent to vendor for product: {ProductName}", details.ProductName);
    }
}