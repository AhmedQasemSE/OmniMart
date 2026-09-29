using System.Threading.Tasks;

namespace OmniMart.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}