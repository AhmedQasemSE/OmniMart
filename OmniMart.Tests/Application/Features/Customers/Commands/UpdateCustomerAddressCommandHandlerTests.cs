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

public class UpdateCustomerAddressCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICustomerProfileRepository> _profileRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<UpdateCustomerAddressCommandHandler>> _loggerMock = new();

    private readonly UpdateCustomerAddressCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public UpdateCustomerAddressCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_profileRepoMock.Object);
        _handler = new UpdateCustomerAddressCommandHandler(
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

        var command = new UpdateCustomerAddressCommand(Guid.NewGuid(), "Home", "Istanbul", "Sisli", "34000", "05554443322");

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

        var command = new UpdateCustomerAddressCommand(Guid.NewGuid(), "Home", "Istanbul", "Sisli", "34000", "05554443322");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainThrowsInvalidOperationException()
    {
        SetupCurrentUser();
        SetupProfileInDb(_defaultUserId); 

        var fakeAddressId = Guid.NewGuid();
        var command = new UpdateCustomerAddressCommand(fakeAddressId, "Home", "Istanbul", "Sisli", "34000", "05554443322");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAddressIsUpdatedSuccessfully()
    {
        SetupCurrentUser();
        var profile = SetupProfileInDb(_defaultUserId);

        var existingAddressId = profile.AddAddress("Old Title", "Old City", "Old Street", "00000", "0000000");

        var command = new UpdateCustomerAddressCommand(existingAddressId, "New Title", "Istanbul", "Levent", "34330", "05559998877");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}