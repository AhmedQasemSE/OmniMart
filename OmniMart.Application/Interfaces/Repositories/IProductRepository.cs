using OmniMart.Domain.Entities; 

namespace OmniMart.Application.Interfaces.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> IsSkuExistsAsync(string sku, CancellationToken cancellationToken = default);
    Task<Product?> GetProductWithVariantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetProductWithAllVariantsForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetDeletedProductWithVariantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default);
    Task<List<string>> GetExistingSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
}


