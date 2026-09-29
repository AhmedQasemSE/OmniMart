using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.EventHandlers;

public record PasswordResetRequestedDTO(string Email);

public interface IPasswordResetQueries
{
    Task<PasswordResetRequestedDTO?> GetUserDetailsForResetAsync(Guid userId);
}

public class PasswordResetEventHandler : INotificationHandler<PasswordResetRequestedEvent>
{
    private readonly IPasswordResetQueries _queries;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration; 
    private readonly ILogger<PasswordResetEventHandler> _logger;

    public PasswordResetEventHandler(
        IPasswordResetQueries queries,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<PasswordResetEventHandler> logger)
    {
        _queries = queries;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(PasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        var userDetails = await _queries.GetUserDetailsForResetAsync(notification.UserId);

        if (userDetails == null || string.IsNullOrWhiteSpace(userDetails.Email))
        {
            _logger.LogWarning("Cannot process password reset. User details or email not found for UserId: {UserId}", notification.UserId);
            return;
        }

        string frontendUrl = _configuration["FrontendSettings:ResetPasswordUrl"] ?? "https://omnimart.com/reset-password";

        string magicLink = $"{frontendUrl}?email={Uri.EscapeDataString(userDetails.Email)}&token={Uri.EscapeDataString(notification.ResetToken)}";

        string subject = "OmniMart - Password Reset Request 🔒";
        string body = $"Hello,\n\n" +
                      $"We received a request to reset your password for your OmniMart account.\n\n" +
                      $"Please click the secure link below to choose a new password:\n\n" +
                      $"{magicLink}\n\n" + 
                      $"This link is valid for 15 minutes. If you did not request this, please ignore this email and your password will remain unchanged.\n\n" +
                      $"Securely yours,\nOmniMart Security Team";

        await _emailService.SendEmailAsync(userDetails.Email, subject, body);
    }
}