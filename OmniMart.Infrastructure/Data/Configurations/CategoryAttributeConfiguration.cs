using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations;

public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {

        builder.Property(x => x.Id).ValueGeneratedNever();


        builder.HasIndex(c => new { c.CategoryId, c.ProductAttributeId }).IsUnique();

        builder.HasOne(c=>c.Category)
            .WithMany(c => c.CategoryAttributes)
            .HasForeignKey(c => c.CategoryId)
              .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ca => ca.ProductAttribute)
               .WithMany(pa => pa.CategoryAttributes)
               .HasForeignKey(pa => pa.ProductAttributeId)
              .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(ca => !ca.Category!.IsDeleted);
    }
}