using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Orders.Queries;
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

namespace OmniMart.Tests.Application.Features.Orders.Queries;

public class GetVendorOrdersQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetVendorOrdersQueryHandler>> _loggerMock = new();

    private readonly GetVendorOrdersQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public GetVendorOrdersQueryHandlerTests()
    {
        _handler = new GetVendorOrdersQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

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
        _currentUserServiceMock.Setup(u => u.UserId).Returns(string.Empty);

        var query = new GetVendorOrdersQuery(null, 1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenVendorProfileDoesNotExist()
    {
        SetupCurrentUser();
        SetupVendors();

        var query = new GetVendorOrdersQuery(null, 1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnAllOrders_WhenNoStatusFilterIsProvided()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendors(vendor);

        var order1 = new OrderBuilder().WithVendorProfile(vendor).WithItem(Guid.NewGuid(), 100m, 1).Build();
        var order2 = new OrderBuilder().WithVendorProfile(vendor).WithItem(Guid.NewGuid(), 200m, 2).Build();

        SetupOrders(order1, order2);

        var query = new GetVendorOrdersQuery(Status: null, PageNumber: 1, PageSize: 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnFilteredOrders_WhenStatusFilterIsProvided()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupVendors(vendor);

        var pendingOrder = new OrderBuilder().WithVendorProfile(vendor).WithStatus(OrderStatus.Pending).Build();
        var shippedOrder = new OrderBuilder().WithVendorProfile(vendor).WithStatus(OrderStatus.Shipped).Build();

        SetupOrders(pendingOrder, shippedOrder);

        var query = new GetVendorOrdersQuery(Status: OrderStatus.Pending, PageNumber: 1, PageSize: 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle(o => o.OrderId == pendingOrder.Id);
    }

    #endregion
}