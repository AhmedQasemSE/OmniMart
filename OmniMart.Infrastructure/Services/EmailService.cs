using Hangfire;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OmniMart.Application.Interfaces;
using OmniMart.Infrastructure.Services.Settings;
using System;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly EmailSettings _emailSettings;

    public EmailService(
        ILogger<EmailService> logger,
        IBackgroundJobClient backgroundJobClient,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _emailSettings = emailSettings.Value;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _backgroundJobClient.Enqueue(() => SendEmailInternal(to, subject, body));
        return Task.CompletedTask;
    }

    public void SendEmailInternal(string to, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using (var client = new SmtpClient())
            {
                client.Connect(_emailSettings.SmtpServer, _emailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(_emailSettings.SenderEmail, _emailSettings.Password);

                client.Send(message);
                client.Disconnect(true);
            }

            _logger.LogInformation("✅ [EMAIL SENT SUCCESSFULLY] To: {To} | Subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL FAILED] Could not send email to: {To}", to);
            throw;
        }
    }
}