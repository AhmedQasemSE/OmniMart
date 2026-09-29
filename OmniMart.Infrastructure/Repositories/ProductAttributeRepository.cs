
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Repositories;

public class ProductAttributeRepository : GenericRepository<ProductAttribute>, IProductAttributeRepository
{
    public ProductAttributeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> IsNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => a.Name == name, cancellationToken);
    }

    public async Task<bool> IsAttributeInUseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool usedInCategory = await _context.CategoryAttributes.AnyAsync(ca => ca.ProductAttributeId == id, cancellationToken);
        bool usedInVariant = await _context.VariantAttributeValues.AnyAsync(va => va.ProductAttributeId == id, cancellationToken);

        return usedInCategory || usedInVariant;
    }
}