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

public class DeleteCustomerAddressCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICustomerProfileRepository> _profileRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<DeleteCustomerAddressCommandHandler>> _loggerMock = new();
    private readonly DeleteCustomerAddressCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public DeleteCustomerAddressCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_profileRepoMock.Object);
        _handler = new DeleteCustomerAddressCommandHandler(
            _unitOfWorkMock.Object, _currentUserServiceMock.Object, _loggerMock.Object);
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

        var command = new DeleteCustomerAddressCommand(Guid.NewGuid());

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

        var command = new DeleteCustomerAddressCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAddressDoesNotExistInProfile()
    {
        SetupCurrentUser();
        SetupProfileInDb(_defaultUserId); 

        var fakeAddressId = Guid.NewGuid();
        var command = new DeleteCustomerAddressCommand(fakeAddressId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        result.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAddressIsDeletedSuccessfully()
    {
        SetupCurrentUser();
        var profile = SetupProfileInDb(_defaultUserId);

        var existingAddressId = profile.AddAddress("Home", "Istanbul", "Sisli", "34000", "05554443322");
        var command = new DeleteCustomerAddressCommand(existingAddressId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}