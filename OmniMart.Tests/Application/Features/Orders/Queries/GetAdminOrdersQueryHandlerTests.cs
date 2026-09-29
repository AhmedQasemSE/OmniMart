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

public class GetAdminOrdersQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetAdminOrdersQueryHandler>> _loggerMock = new();

    private readonly GetAdminOrdersQueryHandler _handler;

    public GetAdminOrdersQueryHandlerTests()
    {
        _handler = new GetAdminOrdersQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods 

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.SuperAdmin.ToString());
    }

    private void SetupData(params Order[] orders)
    {
        var mockOrdersDbSet = orders.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Orders).Returns(mockOrdersDbSet.Object);
    }

    private Order CreateValidOrder(OrderStatus? status = null, DateTimeOffset? date = null, string customerName = "Test", string vendorName = "Test Store")
    {
        var user = new UserBuilder().WithFirstName(customerName).WithLastName("User").Build();
        var customer = new CustomerProfileBuilder().WithUser(user).Build();
        var vendor = new VendorProfileBuilder().WithStoreName(vendorName).Build();

        var builder = new OrderBuilder()
            .WithCustomerEntity(customer)
            .WithVendorProfileEntity(vendor);

        if (status.HasValue) builder.WithStatus(status.Value);
        if (date.HasValue) builder.WithOrderDate(date.Value);

        return builder.Build();
    }

    #endregion

    #region 1. Security & Empty State Tests

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAdminOrSuperAdmin()
    {
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Customer.ToString());

        var query = new GetAdminOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenNoOrdersExist()
    {
        SetupCurrentUser();
        SetupData();

        var query = new GetAdminOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    #endregion

    #region 2. Business Logic & Filtering Tests

    [Fact]
    public async Task Handle_ShouldReturnPaginatedOrders_WithCorrectMapping_WhenOrdersExist()
    {
        SetupCurrentUser();

        var order1 = CreateValidOrder(customerName: "Ahmed", vendorName: "Tech Store");
        var order2 = CreateValidOrder(customerName: "Ali", vendorName: "Fashion Store");

        SetupData(order1, order2);

        var query = new GetAdminOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Data.Should().HaveCount(2);

        var firstDto = result.Value.Data.First(x => x.OrderId == order1.Id);
        firstDto.CustomerName.Should().Be("Ahmed User");
        firstDto.VendorStoreName.Should().Be("Tech Store");
    }

    [Fact]
    public async Task Handle_ShouldFilterOrdersBySearchTerm()
    {
        SetupCurrentUser();

        var targetOrder = CreateValidOrder(customerName: "TargetName");
        var noiseOrder = CreateValidOrder(customerName: "NoiseName");

        SetupData(targetOrder, noiseOrder);

        var query = new GetAdminOrdersQuery(SearchTerm: "targetname");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle(x => x.OrderId == targetOrder.Id);
    }

    [Fact]
    public async Task Handle_ShouldFilterOrdersByStatus()
    {
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Admin.ToString());

        var pendingOrder = CreateValidOrder(status: OrderStatus.Pending);
        var shippedOrder = CreateValidOrder(status: OrderStatus.Shipped);

        SetupData(pendingOrder, shippedOrder);

        var query = new GetAdminOrdersQuery(StatusFilter: OrderStatus.Pending);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle(x => x.OrderId == pendingOrder.Id);
    }

    [Fact]
    public async Task Handle_ShouldFilterOrdersByDateRange()
    {
        SetupCurrentUser();

        var oldOrder = CreateValidOrder(date: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var targetOrder = CreateValidOrder(date: new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        var futureOrder = CreateValidOrder(date: new DateTimeOffset(2026, 1, 30, 0, 0, 0, TimeSpan.Zero));

        SetupData(oldOrder, targetOrder, futureOrder);

        var query = new GetAdminOrdersQuery(
            StartDate: new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            EndDate: new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero)
        );

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle(x => x.OrderId == targetOrder.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPage_WhenPaginationIsRequested()
    {
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Admin.ToString());

        var order1 = CreateValidOrder();
        var order2 = CreateValidOrder();
        var order3 = CreateValidOrder();

        SetupData(order1, order2, order3);

        var query = new GetAdminOrdersQuery(Page: 2, PageSize: 2);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
        result.Value.CurrentPage.Should().Be(2);
        result.Value.Data.Should().HaveCount(1);
    }

    #endregion
}