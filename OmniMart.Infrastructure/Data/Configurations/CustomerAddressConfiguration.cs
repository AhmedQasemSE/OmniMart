using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(50).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(250).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();

        builder.HasOne(ca => ca.Customer)
               .WithMany(c => c.Addresses)
               .HasForeignKey(ca => ca.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(ca => !ca.IsDeleted);

        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}