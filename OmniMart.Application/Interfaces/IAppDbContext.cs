using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OmniMart.Domain.Entities;

namespace OmniMart.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Product> Products { get; }
        DbSet<StaffProfile> StaffProfiles { get; }
        DbSet<Category> Categories { get; }
        DbSet<ProductVariant> ProductVariants { get; }
        DbSet<ProductAttribute> ProductAttributes { get; }
        DbSet<Domain.Entities.CategoryAttribute> CategoryAttributes { get; }
        DbSet<VendorProfile> VendorProfiles { get; }
        DbSet<CustomerProfile> CustomerProfiles { get; }
        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        DbSet<Order> Orders { get; }
        DbSet<ProductReview> ProductReviews { get; }
        DbSet<PaymentGroup> PaymentGroups { get; }
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    }
}