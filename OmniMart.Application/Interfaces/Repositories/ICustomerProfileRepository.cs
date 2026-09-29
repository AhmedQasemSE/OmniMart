using OmniMart.Domain.Entities;

namespace OmniMart.Application.Interfaces.Repositories;

public interface ICustomerProfileRepository: IGenericRepository<CustomerProfile>
{
    Task<Guid?> GetCustomerIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
