using System.Threading.Tasks;

namespace OmniMart.Application.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(string orderId, decimal totalAmount, string customerEmail);
}