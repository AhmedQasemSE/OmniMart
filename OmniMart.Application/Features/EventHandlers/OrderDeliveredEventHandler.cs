using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;

namespace OmniMart.Application.Features.Orders.EventHandlers;


public record OrderDeliveredEventDTOs(string CustomerEmail);

public interface IOrderDeliveredQueries
{
    Task<OrderDeliveredEventDTOs?> GetCustomerContactInfoAsync(Guid orderId);
}

public class OrderDeliveredEventHandler : INotificationHandler<OrderDeliveredEvent>
{
    private readonly IEmailService _emailService;
    private readonly IOrderDeliveredQueries _orderQueries;
    private readonly ILogger<OrderDeliveredEventHandler> _logger;

    public OrderDeliveredEventHandler(
        IEmailService emailService,
        IOrderDeliveredQueries orderQueries,
        ILogger<OrderDeliveredEventHandler> logger)
    {
        _emailService = emailService;
        _orderQueries = orderQueries;
        _logger = logger;
    }

    public async Task Handle(OrderDeliveredEvent notification, CancellationToken cancellationToken)
    {
        var contact = await _orderQueries.GetCustomerContactInfoAsync(notification.OrderId);

        if (contact == null)
        {
            _logger.LogWarning("Customer contact info not found for Delivered Order: {OrderId}", notification.OrderId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(contact.CustomerEmail))
        {
            string subject = "Your Order Has Been Delivered! 📦";
            string body = $"Dear Customer,\n\nGreat news! Your order #{notification.OrderId} has been successfully delivered to your address.\n\nWe hope you enjoy your purchase. Don't forget to review your items on OmniMart!\n\nBest regards,\nOmniMart Team";
            await _emailService.SendEmailAsync(contact.CustomerEmail, subject, body);
        }
    }
}