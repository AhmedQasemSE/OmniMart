using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Queries;
using OmniMart.Infrastructure.Services.Queries;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure.Services.Queries;

public class OrderCancellationQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly OrderCancellationQueries _queries;

    public OrderCancellationQueriesTests()
    {
        _queries = new OrderCancellationQueries(_contextMock.Object);
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
    public async Task GetEmailsAsync_WhenOrderExists_ShouldReturnMappedEmails()
    {
        var customerUser = new UserBuilder().WithEmail("customer@test.com").Build();
        var customerProfile = new CustomerProfileBuilder().WithUser(customerUser).Build();

        var vendorUser = new UserBuilder().WithEmail("vendor@test.com").Build();
        var vendorProfile = new VendorProfileBuilder().WithUser(vendorUser).Build();

        var order = new OrderBuilder()
            .WithCustomerEntity(customerProfile)
            .WithVendorProfileEntity(vendorProfile)
            .Build();

        SetupOrders(order);

        var result = await _queries.GetEmailsAsync(order.Id);

        result.Should().NotBeNull();
        result!.CustomerEmail.Should().Be("customer@test.com");
        result.VendorEmail.Should().Be("vendor@test.com");
    }

    [Fact]
    public async Task GetEmailsAsync_WhenOrderDoesNotExist_ShouldReturnNull()
    {
        SetupOrders(); 

        var result = await _queries.GetEmailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}