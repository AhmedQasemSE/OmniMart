using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Orders.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Queries;

public class GetVendorOrderDetailsQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetVendorOrderDetailsQueryHandler>> _loggerMock = new();
    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly GetVendorOrderDetailsQueryHandler _handler;

    public GetVendorOrderDetailsQueryHandlerTests()
    {
        _handler = new GetVendorOrderDetailsQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods (المُهيئات الافتراضية)

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupVendors(params VendorProfile[] vendors)
    {
        var mockDbSet = vendors.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    private void SetupOrders(params Order[] orders)
    {
        var mockDbSet = orders.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Orders).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsInvalid()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(String.Empty);

        var query = new GetVendorOrderDetailsQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenVendorProfileDoesNotExist()
    {
        SetupCurrentUser();
        SetupVendors(); 

        var query = new GetVendorOrderDetailsQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExistAtAll()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendors(vendor);

        SetupOrders(); 

        var query = new GetVendorOrderDetailsQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderBelongsToAnotherVendor()
    {
        SetupCurrentUser();

        var currentVendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendors(currentVendor);

        var anotherVendor = new VendorProfileBuilder().Build();
        var order = new OrderBuilder().WithVendorProfile(anotherVendor).Build();

        SetupOrders(order);

        var query = new GetVendorOrderDetailsQuery(order.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("permission");
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedDetails_WhenOrderExistsAndBelongsToVendor()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendors(vendor);

        var product = new ProductBuilder().WithName("Super Laptop").Build();
        var variant = new ProductVariantBuilder().WithProductEntity(product).WithSKU("LAP-100").Build();

        var order = new OrderBuilder()
            .WithVendorProfile(vendor)
            .WithItem(variant.Id, 1500m, 2, variant)
            .Build();

        SetupOrders(order);

        var query = new GetVendorOrderDetailsQuery(order.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderId.Should().Be(order.Id);

        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Quantity.Should().Be(2);
        result.Value.Items.First().ProductName.Should().Be("Super Laptop");
        result.Value.Items.First().SKU.Should().Be("LAP-100");
    }

    #endregion
}