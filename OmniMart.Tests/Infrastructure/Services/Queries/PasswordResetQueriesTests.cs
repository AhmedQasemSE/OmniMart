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

public class PasswordResetQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly PasswordResetQueries _queries;

    public PasswordResetQueriesTests()
    {
        _queries = new PasswordResetQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupUsers(params User[] users)
    {
        var mockDbSet = users.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Users).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetUserDetailsForResetAsync_WhenUserExists_ShouldReturnEmail()
    {
        var user = new UserBuilder().WithEmail("reset@omnimart.com").Build();

        SetupUsers(user);

        var result = await _queries.GetUserDetailsForResetAsync(user.Id);

        result.Should().NotBeNull();
        result!.Email.Should().Be("reset@omnimart.com");
    }

    [Fact]
    public async Task GetUserDetailsForResetAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        SetupUsers();

        var result = await _queries.GetUserDetailsForResetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}