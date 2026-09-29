using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.EventHandlers;

public record OrderPlacedEventDTOs(string CustomerEmail, string VendorEmail);

public interface IOrderPlacedEventQueries
{
    Task<OrderPlacedEventDTOs?> GetOrderPlacedEventDetailsAsync(Guid orderId);
}

public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;
    private readonly IOrderPlacedEventQueries _orderQueries;
    private readonly IEmailService _emailService;

    public OrderPlacedEventHandler(
        ILogger<OrderPlacedEventHandler> logger,
        IOrderPlacedEventQueries orderQueries,
        IEmailService emailService)
    {
        _logger = logger;
        _orderQueries = orderQueries;
        _emailService = emailService;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        var emailsDto = await _orderQueries.GetOrderPlacedEventDetailsAsync(notification.OrderId);

        if (emailsDto == null)
        {
            _logger.LogWarning("No contact info found for newly placed Order: {OrderId}", notification.OrderId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(emailsDto.CustomerEmail))
        {
            string customerSubject = "Order Received - OmniMart";
            string customerBody = $"Dear Customer,\n\nThank you for shopping with OmniMart! We have received your order #{notification.OrderId}.\n\nThe total amount is {notification.TotalAmount:C}. We are currently reviewing your order and will notify you once it's confirmed and processing begins.\n\nBest regards,\nOmniMart Team";
            await _emailService.SendEmailAsync(emailsDto.CustomerEmail, customerSubject, customerBody);
        }

        if (!string.IsNullOrWhiteSpace(emailsDto.VendorEmail))
        {
            string vendorSubject = "New Order Alert - Action Required";
            string vendorBody = $"Hello Vendor,\n\nGreat news! You have a new order pending confirmation (Order #{notification.OrderId}).\n\nPlease check your vendor dashboard to review the items and prepare for packaging once the order is fully approved.\n\nThank you,\nOmniMart System";
            await _emailService.SendEmailAsync(emailsDto.VendorEmail, vendorSubject, vendorBody);
        }
    }
}