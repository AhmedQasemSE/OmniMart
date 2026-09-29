using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Services.Queries;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure.Services.Queries;

public class ProductReviewedQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly ProductReviewedQueries _queries;

    public ProductReviewedQueriesTests()
    {
        _queries = new ProductReviewedQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupReviews(params ProductReview[] reviews)
    {
        var mockDbSet = reviews.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.ProductReviews).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetReviewDetailsAsync_WhenReviewExists_ShouldReturnMappedDetails()
    {
        var customerUser = new UserBuilder().WithFirstName("Ahmed").Build();
        var customerProfile = new CustomerProfileBuilder().WithUser(customerUser).Build();

        var vendorUser = new UserBuilder().WithEmail("vendor-review@test.com").Build();
        var vendorProfile = new VendorProfileBuilder().WithUser(vendorUser).Build();
        var product = new ProductBuilder()
            .WithName("Reviewed Product")
            .WithVendorProfile(vendorProfile)
            .Build();

        var review = new ProductReviewBuilder()
            .WithProduct(product)
            .WithCustomer(customerProfile)
            .WithRating(5)
            .Build();

        SetupReviews(review);

        var result = await _queries.GetReviewDetailsAsync(review.Id);

        result.Should().NotBeNull();
        result!.VendorEmail.Should().Be("vendor-review@test.com");
        result.ProductName.Should().Be("Reviewed Product");
        result.Rating.Should().Be(5);
        result.CustomerName.Should().Be("Ahmed");
    }

    [Fact]
    public async Task GetReviewDetailsAsync_WhenReviewDoesNotExist_ShouldReturnNull()
    {
        SetupReviews();

        var result = await _queries.GetReviewDetailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}