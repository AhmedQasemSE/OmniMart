using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Services.Queries;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure.Services.Queries;

public class OrderDeliveredQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly OrderDeliveredQueries _queries;

    public OrderDeliveredQueriesTests()
    {
        _queries = new OrderDeliveredQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupOrders(params Order[] orders)
    {
        var mockDbSet = orders.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Orders).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetCustomerContactInfoAsync_WhenOrderExists_ShouldReturnCustomerEmail()
    {
        var customerUser = new UserBuilder().WithEmail("delivered@test.com").Build();
        var customerProfile = new CustomerProfileBuilder().WithUser(customerUser).Build();

        var order = new OrderBuilder()
            .WithCustomerEntity(customerProfile)
            .Build();

        SetupOrders(order);

        var result = await _queries.GetCustomerContactInfoAsync(order.Id);

        result.Should().NotBeNull();
        result!.CustomerEmail.Should().Be("delivered@test.com");
    }

    [Fact]
    public async Task GetCustomerContactInfoAsync_WhenOrderDoesNotExist_ShouldReturnNull()
    {
        SetupOrders();

        var result = await _queries.GetCustomerContactInfoAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}