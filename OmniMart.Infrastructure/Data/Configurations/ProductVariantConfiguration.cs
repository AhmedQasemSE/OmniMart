using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(v => v.SKU).HasMaxLength(50);
            builder.HasIndex(v => v.SKU).IsUnique();

            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.HasOne(p=>p.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(pv => !pv.Product!.IsDeleted
            && !pv.IsDeleted);
        }
    }
}