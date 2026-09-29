using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.EventHandlers;

public record OrderRefundedEventDTOs(string CustomerEmail, string VendorEmail);

public interface IOrderRefundedQueries
{
    Task<OrderRefundedEventDTOs?> GetCustomerContactInfoAsync(Guid orderId);
}

public class OrderRefundedEventHandler : INotificationHandler<OrderRefundedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IOrderRefundedQueries _orderRefundedQueries;
    private readonly ILogger<OrderRefundedEventHandler> _logger;

    public OrderRefundedEventHandler(
        IEmailService emailService,
        IOrderRefundedQueries orderRefundedQueries,
        ILogger<OrderRefundedEventHandler> logger)
    {
        _emailService = emailService;
        _orderRefundedQueries = orderRefundedQueries;
        _logger = logger;
    }

    public async Task Handle(OrderRefundedEvent notification, CancellationToken cancellationToken)
    {
        var contact = await _orderRefundedQueries.GetCustomerContactInfoAsync(notification.OrderId);

        if (contact == null)
        {
            _logger.LogWarning("Contact info not found for Refunded Order: {OrderId}", notification.OrderId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(contact.CustomerEmail))
        {
            string subject = "Order Refund Processed";
            string body = $"Dear Customer,\n\nYour order #{notification.OrderId} has been refunded with the amount of {notification.RefundAmount:C}.\n\nBest regards,\nOmniMart Team";
            await _emailService.SendEmailAsync(contact.CustomerEmail, subject, body);
        }

        if (!string.IsNullOrWhiteSpace(contact.VendorEmail))
        {
            string subject = "Order Refunded Notice";
            string body = $"Hello Vendor,\n\nOrder #{notification.OrderId} containing your items has been refunded for the amount of {notification.RefundAmount:C}.\n\nOmniMart System";
            await _emailService.SendEmailAsync(contact.VendorEmail, subject, body);
        }
    }
}