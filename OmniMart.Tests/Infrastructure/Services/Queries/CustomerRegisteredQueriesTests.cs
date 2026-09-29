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

public class CustomerRegisteredQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly CustomerRegisteredQueries _queries;

    public CustomerRegisteredQueriesTests()
    {
        _queries = new CustomerRegisteredQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupCustomerProfiles(params CustomerProfile[] profiles)
    {
        var mockDbSet = profiles.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.CustomerProfiles).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetCustomerRegisteredEventDtoAsync_WhenCustomerExists_ShouldReturnEmail()
    {
        var user = new UserBuilder().WithEmail("customer@omnimart.com").Build();
        var profile = new CustomerProfileBuilder().WithUser(user).Build();

        SetupCustomerProfiles(profile);

        var result = await _queries.GetCustomerRegisteredEventDtoAsync(profile.Id);

        result.Should().NotBeNull();
        result!.Email.Should().Be("customer@omnimart.com");
    }

    [Fact]
    public async Task GetCustomerRegisteredEventDtoAsync_WhenCustomerDoesNotExist_ShouldReturnNull()
    {
        SetupCustomerProfiles();

        var result = await _queries.GetCustomerRegisteredEventDtoAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}