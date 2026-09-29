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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Queries;

public class GetCustomerAddressesQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly GetCustomerAddressesQueryHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();

    public GetCustomerAddressesQueryHandlerTests()
    {
        _handler = new GetCustomerAddressesQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupCustomerProfilesDbSet(params CustomerProfile[] profiles)
    {
        var mockDbSet = profiles.ToList().BuildMockDbSet();
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
        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);

        var query = new GetCustomerAddressesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithEmptyList_WhenProfileNotFound()
    {
        SetupCurrentUser();
        SetupCustomerProfilesDbSet();

        var query = new GetCustomerAddressesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithCorrectDtoMapping_WhenAddressesExist()
    {
        SetupCurrentUser();

        var profile = new CustomerProfileBuilder()
            .WithUserId(_defaultUserId)
            .Build();

        var homeAddressId = profile.AddAddress("Home", "Istanbul", "Sisli", "34000", "05554443322");
        var workAddressId = profile.AddAddress("Work", "Ankara", "Cankaya", "06000", "05559998877");

        SetupCustomerProfilesDbSet(profile);

        var query = new GetCustomerAddressesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var addresses = result.Value!.ToList();
        addresses.Should().HaveCount(2);

        var homeDto = addresses.Single(a => a.Id == homeAddressId);
        homeDto.Title.Should().Be("Home");
        homeDto.City.Should().Be("Istanbul");
        homeDto.Street.Should().Be("Sisli");
        homeDto.ZipCode.Should().Be("34000");
        homeDto.PhoneNumber.Should().Be("05554443322");

        var workDto = addresses.Single(a => a.Id == workAddressId);
        workDto.Title.Should().Be("Work");
        workDto.City.Should().Be("Ankara");
    }

    #endregion
}