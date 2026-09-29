namespace OmniMart.Infrastructure.Services.Settings;

public record EmailSettings
{
    public string SmtpServer { get; init; } = string.Empty;
    public int Port { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string SenderEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}