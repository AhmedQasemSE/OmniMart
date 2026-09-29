using OmniMart.Domain.Entities;

namespace OmniMart.Application.Interfaces.Repositories;

public interface IProductReviewRepository : IGenericRepository<ProductReview>
{
    Task<bool> HasCustomerReviewedProductAsync(Guid productId, Guid customerId, CancellationToken cancellationToken = default);
}