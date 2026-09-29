using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
    {
        public void Configure(EntityTypeBuilder<CustomerProfile> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(c => c.ExternalPaymentCustomerId).HasMaxLength(100);

            builder.Property(c => c.TotalSpent).HasColumnType("decimal(18,2)");


            builder.HasOne(u=>u.User)
           .WithOne(c=>c.CustomerProfile)
           .HasForeignKey<CustomerProfile>(c => c.UserId)
           .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(cu => !cu.User!.IsDeleted);

            builder.Navigation(c => c.Addresses)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}