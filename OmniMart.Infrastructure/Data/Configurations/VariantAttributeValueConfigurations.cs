using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
    {
        public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(u => u.Value).HasMaxLength(100);

            builder.HasIndex(v => new { v.ProductVariantId, v.ProductAttributeId }).IsUnique();

            builder.HasOne(pv=>pv.ProductVariant)
                   .WithMany(pv => pv.VariantAttributeValues)
                   .HasForeignKey(v => v.ProductVariantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.ProductAttribute)
                   .WithMany(pa => pa.VariantAttributeValues)
                   .HasForeignKey(v => v.ProductAttributeId)
                .OnDelete(DeleteBehavior.Restrict);


        }
    }
}
