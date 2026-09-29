using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class VendorProfileConfiguration : IEntityTypeConfiguration<VendorProfile>
    {
        public void Configure(EntityTypeBuilder<VendorProfile> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(u => u.StoreName).HasMaxLength(100);

            builder.Property(u => u.CommercialRegisterNumber).HasMaxLength(100);
            builder.HasIndex(u => u.CommercialRegisterNumber).IsUnique();

            builder.Property(v => v.VendorNumber).HasMaxLength(12);
            builder.HasIndex(v => v.VendorNumber).IsUnique();

            builder.Property(v => v.CurrentBalance).HasColumnType("decimal(18,2)");
            builder.Property(u => u.CommissionRate).HasColumnType("decimal(18,2)");


            builder.HasOne(u => u.User)
           .WithOne(v => v.VendorProfile)
           .HasForeignKey<VendorProfile>(v => v.UserId)
           .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(v => !v.User!.IsDeleted);

            builder.Property(o => o.RowVersion).IsRowVersion();
        }
    }
}