using OmniMart.Domain.Entities;

namespace OmniMart.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsAccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
