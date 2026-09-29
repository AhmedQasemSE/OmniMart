using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetVendorProductsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<GetVendorProductsQueryHandler>> _loggerMock = new();

    private readonly GetVendorProductsQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();

    public GetVendorProductsQueryHandlerTests()
    {
        _handler = new GetVendorProductsQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupVendorsDb(params VendorProfile[] vendors)
    {
        var mockDbSet = vendors.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    private void SetupProductsDb(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests - Part 1: Security & Validation

    [Fact]
    public async Task Handle_WhenUserTokenIsInvalid_ShouldReturnUnauthorized()
    {
        SetupCurrentUser();
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);

        var query = new GetVendorProductsQuery(null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenVendorProfileNotFound_ShouldReturnNotFound()
    {
        SetupCurrentUser();
        SetupVendorsDb(); 
        SetupProductsDb();

        var query = new GetVendorProductsQuery(null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region Tests - Part 2: Filters & Business Logic

    [Fact]
    public async Task Handle_WhenIsDeletedIsFalse_ShouldReturnOnlyNonDeletedProducts()
    {
        SetupCurrentUser();
        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendorsDb(vendor);

        var activeProduct = new ProductBuilder().WithVendorProfile(vendor).Build();
        var deletedProduct = new ProductBuilder().WithVendorProfile(vendor).AsDeleted().Build();
        SetupProductsDb(activeProduct, deletedProduct);

        var query = new GetVendorProductsQuery(Status: null, IsDeleted: false);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(activeProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenIsDeletedIsTrue_ShouldReturnOnlyDeletedProducts()
    {
        SetupCurrentUser();
        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendorsDb(vendor);

        var activeProduct = new ProductBuilder().WithVendorProfile(vendor).Build();
        var deletedProduct = new ProductBuilder().WithVendorProfile(vendor).AsDeleted().Build();
        SetupProductsDb(activeProduct, deletedProduct);

        var query = new GetVendorProductsQuery(Status: null, IsDeleted: true);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(deletedProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenNoProductsMatchCriteria_ShouldReturnEmptyPaginatedResult()
    {
        SetupCurrentUser();
        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendorsDb(vendor);
        SetupProductsDb(); 

        var query = new GetVendorProductsQuery(Status: null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenIsDeletedIsNull_ShouldDefaultToNonDeletedProducts()
    {
        SetupCurrentUser();
        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendorsDb(vendor);

        var activeProduct = new ProductBuilder().WithVendorProfile(vendor).Build();
        var deletedProduct = new ProductBuilder().WithVendorProfile(vendor).AsDeleted().Build();
        SetupProductsDb(activeProduct, deletedProduct);

        var query = new GetVendorProductsQuery(Status: null, IsDeleted: null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Id.Should().Be(activeProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenStatusFilterProvided_ShouldReturnMatchingStatus()
    {
        SetupCurrentUser();
        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendorsDb(vendor);

        var activeProduct = new ProductBuilder()
            .WithVendorProfile(vendor)
            .WithStatus(ProductStatus.Active)
            .Build();

        var pendingProduct = new ProductBuilder()
            .WithVendorProfile(vendor)
            .WithStatus(ProductStatus.PendingReview)
            .Build();

        SetupProductsDb(activeProduct, pendingProduct);

        var query = new GetVendorProductsQuery(Status: ProductStatus.PendingReview);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().Status.Should().Be(ProductStatus.PendingReview);
    }

    [Fact]
    public async Task Handle_WhenProductsExist_ShouldMapToDtoAndPaginateCorrectly()
    {
        SetupCurrentUser();
        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendorsDb(vendor);

        var rejectedProduct = new ProductBuilder()
            .WithVendorProfile(vendor)
            .WithStatus(ProductStatus.Rejected)
            .Build();

        SetupProductsDb(rejectedProduct);

        var query = new GetVendorProductsQuery(Status: null, Page: 1, PageSize: 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.Value!.Data.ToList();

        data.Should().HaveCount(1);
        data[0].Id.Should().Be(rejectedProduct.Id);
        data[0].Name.Should().Be(rejectedProduct.Name);
        data[0].Status.Should().Be(ProductStatus.Rejected);
        data[0].RejectionReason.Should().NotBeNullOrEmpty();
    }

    #endregion
}