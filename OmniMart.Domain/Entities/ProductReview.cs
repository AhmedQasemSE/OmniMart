using System;

namespace OmniMart.Domain.Entities;

public class ProductReview
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Product Product { get; private set; } = null!;
    public CustomerProfile Customer { get; private set; } = null!;

    private ProductReview() { }

    public ProductReview(Guid productId, Guid customerId, int rating, string? comment)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5 stars.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        CustomerId = customerId;
        Rating = rating;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }
    public void UpdateReview(int newRating, string? newComment)
    {
        if (newRating < 1 || newRating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(newRating), "Rating must be between 1 and 5 stars.");
        }

        Rating = newRating;
        Comment = newComment;
    }
}