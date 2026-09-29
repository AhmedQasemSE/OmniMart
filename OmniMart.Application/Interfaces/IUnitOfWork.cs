using OmniMart.Application.Interfaces.Repositories;

namespace OmniMart.Application.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        IUserRepository Users { get; }
        ICustomerProfileRepository CustomerProfiles { get; }
        IVendorProfileRepository VendorProfiles { get; }
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IStaffProfileRepository StaffProfiles { get; }
        ICartRepository Carts { get; }
        IOrderRepository Orders { get; }
        IProductReviewRepository ProductReviews { get; }
        IPaymentGroupRepository PaymentGroups { get; }
        IProductAttributeRepository ProductAttributes { get; }
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
