using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.CustomerProfiles.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.CustomerProfiles.Commands;

public class AddCustomerAddressCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICustomerProfileRepository> _profileRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<AddCustomerAddressCommandHandler>> _loggerMock = new();
    private readonly AddCustomerAddressCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public AddCustomerAddressCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_profileRepoMock.Object);
        _handler = new AddCustomerAddressCommandHandler(
                    _unitOfWorkMock.Object,
                    _currentUserServiceMock.Object,
                    _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
    }

    private CustomerProfile SetupProfileInDb(Guid userId)
    {
        var profile = new CustomerProfile(userId);

        _profileRepoMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<CustomerProfile, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<CustomerProfile, object>>[]>()
        )).ReturnsAsync(profile);

        return profile;
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
        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);

        var command = new AddCustomerAddressCommand("Home", "Istanbul", "Sisli", "34000", "05554443322");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCustomerProfileNotFound()
    {
        SetupCurrentUser();

        _profileRepoMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<CustomerProfile, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<CustomerProfile, object>>[]>()
        )).ReturnsAsync((CustomerProfile?)null);

        var command = new AddCustomerAddressCommand("Home", "Istanbul", "Sisli", "34000", "05554443322");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainThrowsInvalidOperationException()
    {
        SetupCurrentUser();
        var profile = SetupProfileInDb(_defaultUserId);

        profile.AddAddress("Home", "Old City", "Old Street", "12345", "0000000");

        var command = new AddCustomerAddressCommand("Home", "Istanbul", "Sisli", "34000", "05554443322");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAddressIsValid()
    {
        SetupCurrentUser();
        SetupProfileInDb(_defaultUserId);

        var command = new AddCustomerAddressCommand("Work", "Istanbul", "Levent", "34330", "05559998877");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty(); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}