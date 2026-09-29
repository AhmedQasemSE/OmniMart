using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;
using System.Collections;

namespace OmniMart.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {

    }

    public async Task<bool> IsSkuExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants.AnyAsync(v => v.SKU == sku, cancellationToken);
    }
    public async Task<Product?> GetProductWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
    public async Task<Product?> GetProductWithAllVariantsForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.Variants)
            .ThenInclude(v => v.VariantAttributeValues)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
    public async Task<Product?> GetDeletedProductWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .IgnoreQueryFilters() 
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted, cancellationToken);
    }
    public async Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, cancellationToken);
    }
    public async Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id)&&!v.IsDeleted)
            .ToListAsync(cancellationToken);
    }
    public async Task<List<string>> GetExistingSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductVariant>() 
            .Where(v => skus.Contains(v.SKU)) 
            .Select(v => v.SKU)
            .ToListAsync(cancellationToken);
    }
}
