using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain;

public class ProductReviewTests
{
    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly Guid _validCustomerId = Guid.NewGuid();

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateSuccessfully()
    {
        var review = new ProductReview(_validProductId, _validCustomerId, 5, "Great!");

        review.Id.Should().NotBeEmpty();
        review.ProductId.Should().Be(_validProductId);
        review.CustomerId.Should().Be(_validCustomerId);
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Great!");

        review.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    [Fact]
    public void Constructor_WhenProductIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new ProductReview(Guid.Empty, _validCustomerId, 5, "Great!");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Product ID cannot be empty.*");
    }

    [Fact]
    public void Constructor_WhenCustomerIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new ProductReview(_validProductId, Guid.Empty, 5, "Great!");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer ID cannot be empty.*");
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Constructor_WhenRatingIsInvalid_ShouldThrowArgumentOutOfRangeException(int invalidRating)
    {
        Action act = () => new ProductReview(_validProductId, _validCustomerId, invalidRating, "Comment");

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*Rating must be between 1 and 5 stars.*");
    }

    [Fact]
    public void UpdateReview_WhenDataIsValid_ShouldUpdateProperties()
    {
        var review = new ProductReview(_validProductId, _validCustomerId, 3, "Okay");

        review.UpdateReview(5, "Updated to Great!");

        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Updated to Great!");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void UpdateReview_WhenRatingIsInvalid_ShouldThrowArgumentOutOfRangeException(int invalidRating)
    {
        var review = new ProductReview(_validProductId, _validCustomerId, 3, "Okay");

        Action act = () => review.UpdateReview(invalidRating, "Comment");

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*Rating must be between 1 and 5 stars.*");
    }
}