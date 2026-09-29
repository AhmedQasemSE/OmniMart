using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;

namespace OmniMart.Application.Features.EventHandlers;

public record VendorRegisteredDTO(
    string VendorEmail,
    string StoreName, 
    List<string> StaffEmails 
);
public interface IVendorRegisteredQueries {
    Task<VendorRegisteredDTO?> GetVendorRegistrationDetailsAsync(Guid id);
}
public class VendorRegisteredEventHandler : INotificationHandler<VendorRegisteredEvent>
{
    private readonly ILogger<VendorRegisteredEventHandler> _logger;
    private readonly IVendorRegisteredQueries _vendorQueries;
    private readonly IEmailService _emailService;

    public VendorRegisteredEventHandler(
        ILogger<VendorRegisteredEventHandler> logger,
        IVendorRegisteredQueries vendorQueries,
        IEmailService emailService)
    {
        _logger = logger;
        _vendorQueries = vendorQueries;
        _emailService = emailService;
    }

    public async Task Handle(VendorRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var registrationDetails = await _vendorQueries.GetVendorRegistrationDetailsAsync(notification.VendorId);

        if (registrationDetails == null)
        {
            _logger.LogWarning("Registration details not found for newly registered Vendor: {VendorId}", notification.VendorId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(registrationDetails.VendorEmail))
        {
            string vendorSubject = "Welcome to OmniMart! Your application is under review.";
            string vendorBody = $"Dear {registrationDetails.StoreName},\n\n" +
                                $"Welcome to the OmniMart family! We are thrilled to have you on board.\n\n" +
                                $"Your registration has been successfully submitted. Our team is currently reviewing your application and documents.\n" +
                                $"We will notify you once your account is fully activated so you can start selling.\n\n" +
                                $"Best regards,\nOmniMart Team";

            await _emailService.SendEmailAsync(registrationDetails.VendorEmail, vendorSubject, vendorBody);
        }

        if (registrationDetails.StaffEmails != null && registrationDetails.StaffEmails.Any())
        {
            foreach (var adminEmail in registrationDetails.StaffEmails)
            {
                string adminSubject = "Action Required: New Vendor Registration";
                string adminBody = $"Hello Admin/Manager,\n\n" +
                                   $"A new vendor has just registered on the platform.\n\n" +
                                   $"- Store Name: {registrationDetails.StoreName}\n" +
                                   $"- Vendor ID: {notification.VendorId}\n\n" +
                                   $"Please log in to the admin dashboard to review their documents and approve the account.\n\n" +
                                   $"System Notification";

                await _emailService.SendEmailAsync(adminEmail, adminSubject, adminBody);
            }
        }
    }
}