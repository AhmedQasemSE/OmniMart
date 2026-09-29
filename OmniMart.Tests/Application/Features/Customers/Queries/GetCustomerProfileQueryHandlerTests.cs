using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Customers.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Queries;

public class GetCustomerProfileQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly GetCustomerProfileQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public GetCustomerProfileQueryHandlerTests()
    {
        _handler = new GetCustomerProfileQueryHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    #region Helper Methods (Using params for clean and condition-free setup)

    private void SetupValidDependencies(params CustomerProfile[] profiles)
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());

        var mockDbSet = profiles.BuildMockDbSet();
        _contextMock.Setup(c => c.CustomerProfiles).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-guid-format")]
    public async Task Handle_ShouldReturnFailure_WhenUserIsUnauthorizedOrInvalidId(string? invalidUserId)
    {
        var query = new GetCustomerProfileQuery();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProfileNotFound()
    {
        var query = new GetCustomerProfileQuery();

        SetupValidDependencies();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Be("Profile not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithCorrectDtoMapping_WhenProfileExists()
    {

        var user = new UserBuilder()
            .WithId(_defaultUserId)
            .WithFirstName("Ahmed")
            .WithLastName("Yaseen")
            .WithEmail("ahmed@example.com")
            .WithPhoneNumber("05551234567")
            .WithAccountNumber("ACC-12345678")
            .Build();

        var profile = new CustomerProfileBuilder()
            .WithUser(user)
            .WithTotalSpent(5000m)
            .WithLoyaltyPoints(500)
            .Build();

        SetupValidDependencies(profile);

        var query = new GetCustomerProfileQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var dto = result.Value;
        dto.CustomerId.Should().Be(profile.Id);
        dto.FirstName.Should().Be("Ahmed");
        dto.LastName.Should().Be("Yaseen");
        dto.Email.Should().Be("ahmed@example.com");
        dto.PhoneNumber.Should().Be("05551234567");
        dto.AccountNumber.Should().Be("ACC-12345678");
        dto.LoyaltyPoints.Should().Be(500);
        dto.TotalSpent.Should().Be(5000m);
    }

    #endregion
}