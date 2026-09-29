using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductAttribute> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(pa => pa.Name).IsRequired().HasMaxLength(100);

            builder.HasIndex(pa => pa.Name).IsUnique();

        }
    }
}