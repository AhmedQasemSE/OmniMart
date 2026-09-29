using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Queries;
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

namespace OmniMart.Tests.Application.Features.Vendors.Queries;

public class GetVendorDashboardStatsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly GetVendorDashboardStatsQueryHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    public GetVendorDashboardStatsQueryHandlerTests()
    {
        _handler = new GetVendorDashboardStatsQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods 

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupVendors(params VendorProfile[] vendors)
    {
        _contextMock.Setup(c => c.VendorProfiles).Returns(vendors.ToList().BuildMockDbSet().Object);
    }

    private void SetupProducts(params Product[] products)
    {
        _contextMock.Setup(c => c.Products).Returns(products.ToList().BuildMockDbSet().Object);
    }

    private void SetupVariants(params ProductVariant[] variants)
    {
        _contextMock.Setup(c => c.ProductVariants).Returns(variants.ToList().BuildMockDbSet().Object);
    }

    private void SetupOrders(params Order[] orders)
    {
        _contextMock.Setup(c => c.Orders).Returns(orders.ToList().BuildMockDbSet().Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-guid-string")]
    public async Task Handle_ShouldReturnFailure_WhenUserIsUnauthorized(string? invalidUserId)
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);
        var query = new GetVendorDashboardStatsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVendorProfileNotFound()
    {
        SetupCurrentUser();
        SetupVendors();

        var query = new GetVendorDashboardStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithZeroStats_WhenNoDataExists()
    {
        var vendorId = Guid.NewGuid();
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder()
            .WithId(vendorId)
            .WithUserId(_defaultUserId)
            .WithCurrentBalance(0m)
            .Build();

        SetupVendors(vendor);
        SetupProducts();
        SetupVariants();
        SetupOrders();

        var query = new GetVendorDashboardStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var stats = result.Value!;
        stats.TotalActiveProducts.Should().Be(0);
        stats.LowStockItemsAlert.Should().Be(0);
        stats.PendingOrders.Should().Be(0);
        stats.CompletedOrders.Should().Be(0);
        stats.TotalHistoricalRevenue.Should().Be(0m);
        stats.CurrentWalletBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithAccurateMath_WhenDataExists()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder()
            .WithUserId(_defaultUserId)
            .WithCurrentBalance(150.5m)
            .Build();
        SetupVendors(vendor);

        var activeProduct = new ProductBuilder()
            .WithVendorIds(vendor.Id, _defaultUserId)
            .WithStatus(ProductStatus.Active)
            .WithVariant("LOW-STOCK", 10m, 3) 
            .WithVariant("HIGH-STOCK", 10m, 10)
            .Build();

        var deletedProduct = new ProductBuilder()
            .WithVendorIds(vendor.Id, _defaultUserId)
            .AsDeleted()
            .Build();

        SetupProducts(activeProduct, deletedProduct);
        SetupVariants(activeProduct.Variants.ToArray()); 

        var pendingOrder = new OrderBuilder().WithVendorId(vendor.Id).WithStatus(OrderStatus.Pending).Build();
        var processingOrder = new OrderBuilder().WithVendorId(vendor.Id).WithStatus(OrderStatus.Processing).Build();

        var deliveredOrder1 = new OrderBuilder().WithVendorId(vendor.Id).WithStatus(OrderStatus.Delivered).WithForcedTotalAmount(100m).Build();
        var deliveredOrder2 = new OrderBuilder().WithVendorId(vendor.Id).WithStatus(OrderStatus.Delivered).WithForcedTotalAmount(100m).Build();

        SetupOrders(pendingOrder, processingOrder, deliveredOrder1, deliveredOrder2);

        var query = new GetVendorDashboardStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stats = result.Value!;
        stats.TotalActiveProducts.Should().Be(1); 
        stats.LowStockItemsAlert.Should().Be(1);  
        stats.PendingOrders.Should().Be(2);       
        stats.CompletedOrders.Should().Be(2);     
        stats.TotalHistoricalRevenue.Should().Be(200m); 
        stats.CurrentWalletBalance.Should().Be(150.5m); 
    }

    #endregion
}