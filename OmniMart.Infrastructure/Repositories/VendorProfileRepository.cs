using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;

namespace OmniMart.Infrastructure.Repositories
{
    public class VendorProfileRepository:GenericRepository<VendorProfile>, IVendorProfileRepository
    {
        public VendorProfileRepository(AppDbContext context): base(context)
        {
        }
        public async Task<bool> IsCommercialRegisterNumberExistsAsync(string commercialRegisterNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(u => u.CommercialRegisterNumber == commercialRegisterNumber, cancellationToken);
        }
        public async Task<Guid?> GetVendorIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(u => u.UserId == userId).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<IEnumerable<VendorProfile>> GetVendorsByIdsAsync(IEnumerable<Guid> vendorIds, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => vendorIds.Contains(v.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
