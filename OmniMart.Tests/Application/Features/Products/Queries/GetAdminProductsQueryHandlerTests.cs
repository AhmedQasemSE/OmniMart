using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Features.Products.Queries.GetAdminProducts;
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

public class GetAdminProductsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly GetAdminProductsQueryHandler _handler;

    public GetAdminProductsQueryHandlerTests()
    {
        _handler = new GetAdminProductsQueryHandler(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupProductsDbSet(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests - Part 1: Basics & Pagination

    [Fact]
    public async Task Handle_WhenNoProductsExist_ShouldReturnEmptyPaginatedResult()
    {
        SetupProductsDbSet();

        var query = new GetAdminProductsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenProductsExist_ShouldMapToDtoCorrectly()
    {
        var product = new ProductBuilder().Build();
        SetupProductsDbSet(product);

        var query = new GetAdminProductsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.Value!.Data.ToList();

        data.Should().HaveCount(1);
        data[0].Id.Should().Be(product.Id);
        data[0].Name.Should().Be(product.Name);
        data[0].VendorId.Should().Be(product.VendorId);
        data[0].VendorStoreName.Should().NotBe("N/A");
    }

    [Fact]
    public async Task Handle_WhenPaginationIsRequested_ShouldReturnCorrectPage()
    {
        var p1 = new ProductBuilder().WithName("Product 1").Build();
        var p2 = new ProductBuilder().WithName("Product 2").Build();
        var p3 = new ProductBuilder().WithName("Product 3").Build();

        SetupProductsDbSet(p1, p2, p3);

        var query = new GetAdminProductsQuery(Page: 2, PageSize: 2);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.CurrentPage.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Data.Should().HaveCount(1);
    }

    #endregion

    #region Tests - Part 2: Filters 

    [Fact]
    public async Task Handle_WhenSearchTermIsProvided_ShouldFilterByNameOrSku()
    {
        var targetProduct = new ProductBuilder()
            .WithName("Apple iPhone 14")
            .WithVariant("SKU-IPHONE", 1000, 10)
            .Build();

        var otherProduct = new ProductBuilder().WithName("Samsung Galaxy").Build();

        SetupProductsDbSet(targetProduct, otherProduct);

        var queryByName = new GetAdminProductsQuery(SearchTerm: "iphone");
        var resultByName = await _handler.Handle(queryByName, CancellationToken.None);

        resultByName.Value!.Data.Should().HaveCount(1);
        resultByName.Value.Data.First().Id.Should().Be(targetProduct.Id);

        var queryBySku = new GetAdminProductsQuery(SearchTerm: "sku-iphone");
        var resultBySku = await _handler.Handle(queryBySku, CancellationToken.None);

        resultBySku.Value!.Data.Should().HaveCount(1);
        resultBySku.Value.Data.First().Id.Should().Be(targetProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenVendorIdFilterIsProvided_ShouldReturnOnlyVendorProducts()
    {
        var targetProduct = new ProductBuilder().Build();
        var otherProduct = new ProductBuilder().Build();
        var targetVendorId = targetProduct.VendorId;

        SetupProductsDbSet(targetProduct, otherProduct);
        var query = new GetAdminProductsQuery(VendorId: targetVendorId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().VendorId.Should().Be(targetVendorId);
    }

    [Fact]
    public async Task Handle_WhenStatusFilterIsProvided_ShouldReturnOnlyMatchingStatus()
    {
        var draftProduct = new ProductBuilder().WithStatus(ProductStatus.Draft).Build();
        var activeProduct = new ProductBuilder().Build();

        SetupProductsDbSet(draftProduct, activeProduct);
        var query = new GetAdminProductsQuery(Status: ProductStatus.Active);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenIsPublishedFilterIsProvided_ShouldReturnOnlyPublished()
    {
        var publishedProduct = new ProductBuilder().IsPublished(true).Build();
        var unpublishedProduct = new ProductBuilder().IsPublished(false).Build();

        SetupProductsDbSet(publishedProduct, unpublishedProduct);
        var query = new GetAdminProductsQuery(IsPublished: true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().IsPublished.Should().BeTrue();
    }

    #endregion
}