using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Reviews.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Queries;

public class GetProductReviewsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<GetProductReviewsQueryHandler>> _loggerMock = new();
    private readonly GetProductReviewsQueryHandler _handler;

    private readonly Guid _defaultProductId = Guid.NewGuid();

    public GetProductReviewsQueryHandlerTests()
    {
        _handler = new GetProductReviewsQueryHandler(_contextMock.Object, _loggerMock.Object);
    }

    #region Helper Methods 

    private void SetupProducts(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    private void SetupReviews(params ProductReview[] reviews)
    {
        var mockDbSet = reviews.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.ProductReviews).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        SetupProducts();
        var query = new GetProductReviewsQuery(_defaultProductId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Product not found");
    }

    [Fact]
    public async Task Handle_WhenProductExistsButHasNoReviews_ShouldReturnEmptyPaginatedResult()
    {
        var product = new ProductBuilder().WithId(_defaultProductId).Build();
        SetupProducts(product);
        SetupReviews(); 

        var query = new GetProductReviewsQuery(_defaultProductId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalReviews.Should().Be(0);
        result.Value.AverageRating.Should().Be(0.0);
        result.Value.Reviews.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenProductHasReviews_ShouldReturnSummaryAndPaginatedData()
    {
        var product = new ProductBuilder().WithId(_defaultProductId).Build();
        SetupProducts(product);

        var user = new UserBuilder().WithFirstName("Ali").WithLastName("Salem").Build();
        var customer = new CustomerProfileBuilder().WithUser(user).Build();

        var review1 = new ProductReviewBuilder()
            .WithProduct(product)
            .WithCustomer(customer)
            .WithRating(5)
            .WithComment("Amazing!")
            .WithCreatedAt(DateTime.UtcNow.AddMinutes(1))
            .Build();

        var review2 = new ProductReviewBuilder()
            .WithProduct(product)
            .WithCustomer(customer)
            .WithRating(3)
            .WithComment("Average")
            .WithCreatedAt(DateTime.UtcNow)
            .Build();

        SetupReviews(review1, review2);

        var query = new GetProductReviewsQuery(_defaultProductId, Page: 1, PageSize: 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalReviews.Should().Be(2);
        result.Value.AverageRating.Should().Be(4.0);
        result.Value.Reviews.Data.Should().HaveCount(2);

        var firstDto = result.Value.Reviews.Data.First();
        firstDto.CustomerName.Should().Be("Ali Salem");
        firstDto.Rating.Should().Be(5);
        firstDto.Comment.Should().Be("Amazing!");
    }
    #endregion
}