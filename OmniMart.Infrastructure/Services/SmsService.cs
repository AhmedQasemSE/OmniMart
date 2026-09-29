using Hangfire;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SmsService(ILogger<SmsService> logger, IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    public Task SendSmsAsync(string phoneNumber, string message)
    {
        _backgroundJobClient.Enqueue(() => SendSmsInternal(phoneNumber, message));
        return Task.CompletedTask;
    }

    public void SendSmsInternal(string phoneNumber, string message)
    {
        _logger.LogInformation("📱 [SMS SENT VIA HANGFIRE] To: {Phone} | Message: {Message}", phoneNumber, message);

    }
}