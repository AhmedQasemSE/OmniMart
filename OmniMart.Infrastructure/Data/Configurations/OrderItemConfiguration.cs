using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(u => u.Price).HasColumnType("decimal(18,2)");

            builder.HasOne(o=>o.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(o => o.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(oi => oi.ProductVariant)
                   .WithMany(pv => pv.OrderItems)
                   .HasForeignKey(o => o.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

        } 
    }
}
