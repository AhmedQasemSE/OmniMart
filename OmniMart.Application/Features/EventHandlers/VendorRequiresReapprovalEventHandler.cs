using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.EventHandlers;

public record VendorRequiresReapprovalDTO(
    string StoreName,
    List<string> StaffEmails
);

public interface IVendorRequiresReapprovalQueries
{
    Task<VendorRequiresReapprovalDTO?> GetVendorAndStaffDetailsAsync(Guid vendorId);
}

public class VendorRequiresReapprovalEventHandler : INotificationHandler<VendorRequiresReapprovalEvent>
{
    private readonly ILogger<VendorRequiresReapprovalEventHandler> _logger;
    private readonly IVendorRequiresReapprovalQueries _queries;
    private readonly IEmailService _emailService;

    public VendorRequiresReapprovalEventHandler(
        ILogger<VendorRequiresReapprovalEventHandler> logger,
        IVendorRequiresReapprovalQueries queries,
        IEmailService emailService)
    {
        _logger = logger;
        _queries = queries;
        _emailService = emailService;
    }

    public async Task Handle(VendorRequiresReapprovalEvent notification, CancellationToken cancellationToken)
    {
        var details = await _queries.GetVendorAndStaffDetailsAsync(notification.VendorId);

        if (details == null || details.StaffEmails == null || !details.StaffEmails.Any())
        {
            _logger.LogWarning("No staff emails found to notify for Vendor ID: {VendorId}", notification.VendorId);
            return;
        }

        foreach (var adminEmail in details.StaffEmails)
        {
            string subject = "Action Required: Vendor Requires Re-approval";
            string body = $"Hello Admin/Manager,\n\n" +
                          $"Vendor '{details.StoreName}' (ID: {notification.VendorId}) has updated their Commercial Register Number.\n\n" +
                          $"Old Number: {notification.OldCommercialRegisterNumber}\n" +
                          $"New Number: {notification.NewCommercialRegisterNumber}\n\n" +
                          $"Their account has been automatically set to 'Unapproved' for security reasons.\n" +
                          $"Please log in to the admin dashboard to review their new documents and approve the account.\n\n" +
                          $"System Notification";

            await _emailService.SendEmailAsync(adminEmail, subject, body);
        }

        _logger.LogInformation("Re-approval notifications sent to admins for Vendor ID: {VendorId}", notification.VendorId);
    }
}