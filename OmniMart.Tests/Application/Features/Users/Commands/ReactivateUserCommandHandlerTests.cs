using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Users.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders; 
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Users.Commands;

public class ReactivateUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<ReactivateUserCommandHandler>> _loggerMock = new();

    private readonly ReactivateUserCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public ReactivateUserCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _handler = new ReactivateUserCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods 

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString()); 
    }
    private void SetupTargetUserInDb(User targetUser)
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(targetUser.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(targetUser);
    }

    #endregion

    #region 1. Security & Authorization Tests

    [Theory]
    [InlineData("Customer")]
    [InlineData("Vendor")]
    [InlineData("Staff")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_ShouldReturnFailure_WhenCallerIsNotAdminOrSuperAdmin(string? invalidRole)
    {
        SetupCurrentUser();

        _currentUserServiceMock.Setup(s => s.Role).Returns(invalidRole);
        var command = new ReactivateUserCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Only Administrators can reactivate users");
    }

    #endregion

    #region 2. Business Logic Tests

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTargetUserNotFound()
    {
        SetupCurrentUser();

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var command = new ReactivateUserCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsAlreadyActive()
    {
        var targetUser = new UserBuilder().WithRole(SystemRole.Customer).Build();

        SetupTargetUserInDb(targetUser);
        SetupCurrentUser();

        var command = new ReactivateUserCommand(targetUser.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already active");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region 3. Success Path

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAdminReactivatesSuspendedUser()
    {
        var targetUser = new UserBuilder()
                            .WithRole(SystemRole.Customer)
                            .AsSuspended("Old violation")
                            .Build();

        SetupTargetUserInDb(targetUser);
        SetupCurrentUser();

        var command = new ReactivateUserCommand(targetUser.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.IsActive.Should().BeTrue();
        targetUser.SuspensionReason.Should().BeNull(); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}