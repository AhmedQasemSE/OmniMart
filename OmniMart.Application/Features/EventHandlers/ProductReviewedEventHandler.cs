using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Features.EventHandlers;

public record ProductReviewedEventDTO(
    string VendorEmail,
    string ProductName,
    int Rating,
    string CustomerName);

public interface IProductReviewedQueries
{
    Task<ProductReviewedEventDTO?> GetReviewDetailsAsync(Guid reviewId);
}
public class ProductReviewedEventHandler : INotificationHandler<ProductReviewedEvent>
{
    private readonly IProductReviewedQueries _queries;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductReviewedEventHandler> _logger;

    public ProductReviewedEventHandler(
        IProductReviewedQueries queries,
        IEmailService emailService,
        ILogger<ProductReviewedEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ProductReviewedEvent notification, CancellationToken cancellationToken)
    {
        var details = await _queries.GetReviewDetailsAsync(notification.ReviewId);

        if (details == null || string.IsNullOrWhiteSpace(details.VendorEmail))
        {
            _logger.LogWarning("Review details not found for ReviewId: {ReviewId}", notification.ReviewId);
            return;
        }

        string reaction = details.Rating >= 4 ? "Great job!" : "Take a look and see how you can improve.";
        string subject = $"New {details.Rating}-Star Review for '{details.ProductName}'";

        string body = $@"Hello,

         You have received a new review on OmniMart!

         Customer '{details.CustomerName}' just left a {details.Rating}-star review for your product: '{details.ProductName}'.
          {reaction}

         Log in to your vendor dashboard to read the full comment and reply to the customer.

         Best Regards,
         The OmniMart Team";

        await _emailService.SendEmailAsync(details.VendorEmail, subject, body);
        _logger.LogInformation("Review notification sent to vendor for product: {ProductName}", details.ProductName);
    }
}