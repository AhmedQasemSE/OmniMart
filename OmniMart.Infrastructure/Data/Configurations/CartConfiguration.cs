using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasOne(c => c.Customer)
                   .WithOne(cp => cp.Cart)
                   .HasForeignKey<Cart>(c => c.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c=>!c.Customer!.User!.IsDeleted);
    }
   
}