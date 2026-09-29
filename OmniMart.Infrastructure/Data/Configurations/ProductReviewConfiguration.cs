using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Comment)
               .HasMaxLength(1000);

        builder.HasOne(pr => pr.Product)
               .WithMany(p => p.Reviews)
               .HasForeignKey(pr => pr.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Customer)
               .WithMany()
               .HasForeignKey(pr => pr.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pr => new { pr.ProductId, pr.CustomerId })
               .IsUnique();
    }
}