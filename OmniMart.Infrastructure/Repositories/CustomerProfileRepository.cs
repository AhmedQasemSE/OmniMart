using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;

namespace OmniMart.Infrastructure.Repositories;

public class CustomerProfileRepository : GenericRepository<CustomerProfile>, ICustomerProfileRepository
{
    public CustomerProfileRepository(AppDbContext context) : base(context)
    {
    }
    public async Task<Guid?> GetCustomerIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
