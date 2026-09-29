using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.HasOne(ci=>ci.Cart)
                   .WithMany(c => c.CartItems)
                   .HasForeignKey(ci => ci.CartId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ci=>ci.ProductVariant)
                   .WithMany(pv=>pv.CartItems)
                   .HasForeignKey(ci => ci.ProductVariantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(c => !c.ProductVariant!.Product!.IsDeleted
            && !c.ProductVariant.IsDeleted 
            && !c.Cart!.Customer!.User!.IsDeleted);

        }
    }
}