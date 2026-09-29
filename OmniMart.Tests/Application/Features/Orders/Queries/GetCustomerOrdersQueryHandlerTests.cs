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

public class GetCustomerOrdersQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetCustomerOrdersQueryHandler>> _loggerMock = new();
     
    private readonly Guid userId = Guid.NewGuid();
    private readonly GetCustomerOrdersQueryHandler _handler;

    public GetCustomerOrdersQueryHandlerTests()
    {
        _handler = new GetCustomerOrdersQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods (المُهيئات الافتراضية)

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(userId.ToString());
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
        _currentUserServiceMock.Setup(u => u.UserId).Returns(string.Empty);

        var query = new GetCustomerOrdersQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCustomerProfileDoesNotExist()
    {
        SetupCurrentUser();
        SetupCustomers(); 

        var query = new GetCustomerOrdersQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Customer profile not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenNoOrdersExist()
    {
        SetupCurrentUser();

        var customer = new CustomerProfileBuilder().WithUserId(userId).Build();
        SetupCustomers(customer);

        SetupOrders(); 

        var query = new GetCustomerOrdersQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedOrders_WhenConditionsAreMet()
    {
        SetupCurrentUser();

        var customer = new CustomerProfileBuilder().WithUserId(userId).Build();
        SetupCustomers(customer);

        var order1 = new OrderBuilder().WithCustomer(customer).WithItem(Guid.NewGuid(), 100m, 1).Build();
        var order2 = new OrderBuilder().WithCustomer(customer).WithItem(Guid.NewGuid(), 200m, 2).Build();
        var order3 = new OrderBuilder().WithCustomer(customer).WithItem(Guid.NewGuid(), 300m, 3).Build();

        SetupOrders(order1, order2, order3);

        var query = new GetCustomerOrdersQuery(PageNumber: 1, PageSize: 2);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value!.Data.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.CurrentPage.Should().Be(1);
        result.Value.TotalPages.Should().Be(2);

        result.Value.Data.First().TotalItemsCount.Should().BeGreaterThan(0);
    }

    #endregion
}