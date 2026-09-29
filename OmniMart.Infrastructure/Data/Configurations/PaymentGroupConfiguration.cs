using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations;

public class PaymentGroupConfiguration : IEntityTypeConfiguration<PaymentGroup>
{
    public void Configure(EntityTypeBuilder<PaymentGroup> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");

        builder.Property(p => p.StripeSessionId).HasMaxLength(250);

        builder.HasMany(p => p.Orders)
               .WithOne(o => o.PaymentGroup)
               .HasForeignKey(o => o.PaymentGroupId)
               .OnDelete(DeleteBehavior.Restrict); 
    }
}