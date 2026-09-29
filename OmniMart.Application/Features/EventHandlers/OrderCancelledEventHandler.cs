using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.EventHandlers;

public record OrderCancellationEmailsDto(string CustomerEmail, string VendorEmail);

public interface IOrderCancellationQueries
{
    Task<OrderCancellationEmailsDto?> GetEmailsAsync(Guid orderId);
}

public class OrderCancelledEventHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly IEmailService _emailService;
    private readonly IOrderCancellationQueries _orderQueries;
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(
        IEmailService emailService,
        IOrderCancellationQueries orderQueries,
        ILogger<OrderCancelledEventHandler> logger)
    {
        _emailService = emailService;
        _orderQueries = orderQueries;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        var emailsDto = await _orderQueries.GetEmailsAsync(notification.OrderId);

        if (emailsDto == null)
        {
            _logger.LogWarning("No contact info found for Cancelled Order: {OrderId}", notification.OrderId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(emailsDto.CustomerEmail))
        {
            string subject = "Order Cancelled - OmniMart";
            string body = $"Dear Customer,\n\nYour order #{notification.OrderId} has been successfully cancelled as requested. If you have paid, the amount of {notification.TotalAmount:C} will be refunded according to our policy.\n\nBest regards,\nOmniMart Team";
            await _emailService.SendEmailAsync(emailsDto.CustomerEmail, subject, body);
        }

        if (!string.IsNullOrWhiteSpace(emailsDto.VendorEmail))
        {
            string subject = "Stop Packaging - Order Cancelled";
            string body = $"Hello Vendor,\n\nPlease stop processing and packaging order #{notification.OrderId} as it has been cancelled.\n\nThank you,\nOmniMart System";
            await _emailService.SendEmailAsync(emailsDto.VendorEmail, subject, body);
        }
    }
}