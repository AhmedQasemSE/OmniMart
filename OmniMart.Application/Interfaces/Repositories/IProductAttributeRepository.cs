
using OmniMart.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Interfaces.Repositories;

public interface IProductAttributeRepository : IGenericRepository<ProductAttribute>
{
    Task<bool> IsNameExistsAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> IsAttributeInUseAsync(Guid id, CancellationToken cancellationToken = default);
}