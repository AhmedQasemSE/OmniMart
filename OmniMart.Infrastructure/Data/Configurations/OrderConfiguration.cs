using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(o => o.PaymentTransactionId).HasMaxLength(100);

            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.OwnsOne(o => o.ShippingAddress, a =>
            {
                a.Property(p => p.City).HasMaxLength(100).IsRequired();
                a.Property(p => p.Street).HasMaxLength(250).IsRequired();
                a.Property(p => p.ZipCode).HasMaxLength(20);
                a.Property(p => p.PhoneNumber).HasMaxLength(20).IsRequired();
            });

            builder.HasOne(o => o.Customer)
                   .WithMany(c => c.Orders)
                   .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Property(o => o.RowVersion).IsRowVersion();

            builder.HasOne(o => o.PaymentGroup)
                   .WithMany(p => p.Orders)
                   .HasForeignKey(o => o.PaymentGroupId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}