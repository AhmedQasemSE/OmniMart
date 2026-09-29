using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;
using OmniMart.Infrastructure.Repositories;

namespace OmniMart.Infrastructure.Data.Configurations;

public class ProductReviewRepository : GenericRepository<ProductReview>, IProductReviewRepository
{
    public ProductReviewRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> HasCustomerReviewedProductAsync(Guid productId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(r => r.ProductId == productId && r.CustomerId == customerId, cancellationToken);
    }
}