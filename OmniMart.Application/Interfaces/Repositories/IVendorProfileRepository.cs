using OmniMart.Domain.Entities;

namespace OmniMart.Application.Interfaces.Repositories;

public interface IVendorProfileRepository : IGenericRepository<VendorProfile>
{
    Task<bool> IsCommercialRegisterNumberExistsAsync(string commercialRegisterNumber, CancellationToken cancellationToken = default);
    Task<Guid?> GetVendorIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<VendorProfile>> GetVendorsByIdsAsync(IEnumerable<Guid> vendorIds, CancellationToken cancellationToken = default);
}

