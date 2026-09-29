using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Queries.GetProducts;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetProductsQueryHandler>> _loggerMock = new();
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _handler = new GetProductsQueryHandler(_contextMock.Object, _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupProductsDbSet(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests - Part 1: Base Query & Pagination

    [Fact]
    public async Task Handle_WhenNoProductsMatchBaseFilters_ShouldReturnEmptyResult()
    {
        var hiddenProduct = new ProductBuilder().IsPublished(false).Build();

        SetupProductsDbSet(hiddenProduct);
        var query = new GetProductsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenProductIsDeleted_ShouldNotReturnItEvenIfActive()
    {
        var deletedProduct = new ProductBuilder().AsDeleted().Build();

        SetupProductsDbSet(deletedProduct);
        var query = new GetProductsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenValidProductsExist_ShouldApplyPaginationAndMapProperly()
    {
        var p1 = new ProductBuilder().Build();
        var p2 = new ProductBuilder().Build();
        var p3 = new ProductBuilder().Build();

        SetupProductsDbSet(p1, p2, p3);
        var query = new GetProductsQuery(Page: 2, PageSize: 2);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.CurrentPage.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);

        result.Value.Data.Should().HaveCount(1);
        var dto = result.Value.Data.First();
        dto.Name.Should().NotBeNullOrEmpty();
        dto.Status.Should().Be(ProductStatus.Active);
    }

    #endregion

    #region Tests - Part 2: Filters (Search, Category, Price)

    [Fact]
    public async Task Handle_WhenSearchTermProvided_ShouldFilterByName()
    {
        var targetProduct = new ProductBuilder().WithName("Gaming Laptop Pro").Build();
        var otherProduct = new ProductBuilder().WithName("Office Mouse").Build();

        SetupProductsDbSet(targetProduct, otherProduct);
        var query = new GetProductsQuery(SearchTerm: "laptop");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(targetProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenCategoryIdProvided_ShouldFilterByCategory()
    {
        var targetCategoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();

        var targetProduct = new ProductBuilder().WithCategoryId(targetCategoryId).Build();
        var otherProduct = new ProductBuilder().WithCategoryId(otherCategoryId).Build();

        SetupProductsDbSet(targetProduct, otherProduct);
        var query = new GetProductsQuery(CategoryId: targetCategoryId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(targetProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenPriceRangeProvided_ShouldFilterByVariantPrices()
    {
        var cheapProduct = new ProductBuilder()
            .WithVariant("SKU-1", 50m, 10)
            .Build();

        var middleProduct = new ProductBuilder()
            .WithVariant("SKU-2", 200m, 10)
            .Build();

        var superExpensiveProduct = new ProductBuilder()
            .WithVariant("SKU-3", 500m, 10)
            .Build();

        SetupProductsDbSet(cheapProduct, middleProduct, superExpensiveProduct);

        var query = new GetProductsQuery(MinPrice: 100m, MaxPrice: 300m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(middleProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenOnlyMinPriceProvided_ShouldFilterCorrectly()
    {
        var cheapProduct = new ProductBuilder()
            .WithVariant("SKU-1", 50m, 10)
            .Build();

        var expensiveProduct = new ProductBuilder()
            .WithVariant("SKU-2", 200m, 10)
            .Build();

        SetupProductsDbSet(cheapProduct, expensiveProduct);
        var query = new GetProductsQuery(MinPrice: 100m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(expensiveProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenOnlyMaxPriceProvided_ShouldFilterCorrectly()
    {
        var cheapProduct = new ProductBuilder()
            .WithVariant("SKU-1", 50m, 10)
            .Build();

        var expensiveProduct = new ProductBuilder()
            .WithVariant("SKU-2", 200m, 10)
            .Build();

        SetupProductsDbSet(cheapProduct, expensiveProduct);
        var query = new GetProductsQuery(MaxPrice: 100m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(cheapProduct.Id);
    }
    #endregion
}