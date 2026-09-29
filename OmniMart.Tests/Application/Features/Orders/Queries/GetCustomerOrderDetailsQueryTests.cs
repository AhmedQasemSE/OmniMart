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

public class GetCustomerOrderDetailsQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly GetCustomerOrderDetailsQueryHandler _handler;
    private readonly Guid _UserId = Guid.NewGuid();
    public GetCustomerOrderDetailsQueryHandlerTests()
    {
        _handler = new GetCustomerOrderDetailsQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods 

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_UserId.ToString());
    }

    private void SetupCustomers(params CustomerProfile[] customers)
    {
        var mockDbSet = customers.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.CustomerProfiles).Returns(mockDbSet.Object);
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

        var query = new GetCustomerOrderDetailsQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCustomerProfileDoesNotExist()
    {
        SetupCurrentUser();
        SetupCustomers();

        var query = new GetCustomerOrderDetailsQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        SetupCurrentUser();

        var customer = new CustomerProfileBuilder().WithUserId(_UserId).Build();
        SetupCustomers(customer);

        SetupOrders(); 

        var query = new GetCustomerOrderDetailsQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderDetails_WhenAllConditionsAreMet()
    {
        SetupCurrentUser();

        var customer = new CustomerProfileBuilder().WithUserId(_UserId).Build();
        SetupCustomers(customer);

        var product = new ProductBuilder().WithName("Test Product").Build();
        var variant = new ProductVariantBuilder().WithProductEntity(product).Build();

        var order = new OrderBuilder()
            .WithCustomerEntity(customer)
            .WithItem(variant.Id, 100m, 2, variant)
            .Build();

        SetupOrders(order);

        var query = new GetCustomerOrderDetailsQuery(order.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderId.Should().Be(order.Id);

        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Quantity.Should().Be(2);
        result.Value.Items.First().ProductName.Should().Be("Test Product");
    }

    #endregion
}