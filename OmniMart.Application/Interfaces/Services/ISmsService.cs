using System.Threading.Tasks;

namespace OmniMart.Application.Interfaces;

public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
}