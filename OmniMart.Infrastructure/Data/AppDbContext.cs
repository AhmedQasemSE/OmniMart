using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data
{
    public class AppDbContext: DbContext , IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
        public DbSet<User> Users { get;set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<VendorProfile> VendorProfiles { get; set; }
        public DbSet<StaffProfile> StaffProfiles { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<CategoryAttribute> CategoryAttributes { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<PaymentGroup> PaymentGroups { get; set; }
    }
}
