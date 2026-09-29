using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(p => p.Name).HasMaxLength(200);
            builder.Property(p => p.Description).HasMaxLength(2000);

            builder.Property(p => p.BasePrice).HasColumnType("decimal(18,2)");

            builder.HasOne(c=>c.Category)
                .WithMany(p=>p.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(vp=>vp.VendorProfile)
                .WithMany(p=>p.Products)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Property(p => p.RowVersion)
           .IsRowVersion();
        }

    }
}
