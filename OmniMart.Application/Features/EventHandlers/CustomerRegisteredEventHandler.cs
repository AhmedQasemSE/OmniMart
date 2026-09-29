using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events; 
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.EventHandlers;

public record CustomerRegisteredEventDto(string Email);

public interface ICustomerRegisteredQueries
{
    Task<CustomerRegisteredEventDto?> GetCustomerRegisteredEventDtoAsync(Guid id);
}

public class CustomerRegisteredEventHandler : INotificationHandler<CustomerRegisteredEvent>
{
    private readonly ICustomerRegisteredQueries _queries;
    private readonly IEmailService _emailService;
    private readonly ILogger<CustomerRegisteredEventHandler> _logger; 
    public CustomerRegisteredEventHandler(
        ICustomerRegisteredQueries queries,
        IEmailService emailService,
        ILogger<CustomerRegisteredEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(CustomerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var result = await _queries.GetCustomerRegisteredEventDtoAsync(notification.CustomerId);

        if (result == null)
        {
            _logger.LogWarning("Customer details not found for ID: {CustomerId}", notification.CustomerId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Email))
        {
            string subject = "Welcome to OmniMart! 🎉";
            string body = $"Dear Customer,\n\n" +
                          $"Welcome to OmniMart! Your account has been successfully created.\n\n" +
                          $"We are thrilled to have you with us. Start exploring our wide range of products and enjoy a seamless shopping experience.\n\n" +
                          $"(If account activation is required, please click the link below to verify your email address: [ActivationLink])\n\n" +
                          $"Happy Shopping,\nOmniMart Team";

            await _emailService.SendEmailAsync(result.Email, subject, body);
        }
    }
}