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

public class SuspendUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<SuspendUserCommandHandler>> _loggerMock;

    private readonly SuspendUserCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public SuspendUserCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<SuspendUserCommandHandler>>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new SuspendUserCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods 

    private User CreateTargetUser(SystemRole role = SystemRole.Customer)
    {
        return new UserBuilder().WithRole(role).Build();
    }
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
        var command = new SuspendUserCommand(Guid.NewGuid(), "Violation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Only Administrators can suspend");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserAttemptsToSuspendSelf()
    {
        var targetUser = CreateTargetUser(SystemRole.Admin);
        SetupTargetUserInDb(targetUser);

        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(targetUser.Id.ToString());

        var command = new SuspendUserCommand(targetUser.Id, "Testing self suspend");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("cannot suspend your own account");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTargetIsSuperAdmin()
    {
        var targetUser = CreateTargetUser(SystemRole.SuperAdmin);
        SetupTargetUserInDb(targetUser);

        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.SuperAdmin.ToString());
        var command = new SuspendUserCommand(targetUser.Id, "Try to suspend boss");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("SuperAdmin account cannot be suspended");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAdminAttemptsToSuspendAnotherAdmin()
    {
        var targetUser = CreateTargetUser(SystemRole.Admin);
        SetupTargetUserInDb(targetUser);

        SetupCurrentUser();

        var command = new SuspendUserCommand(targetUser.Id, "Admin conflict");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("not have permission to suspend another Administrator");
    }

    #endregion

    #region 2. Business Logic Tests 

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTargetUserNotFound()
    {
        SetupCurrentUser();

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var command = new SuspendUserCommand(Guid.NewGuid(), "Violation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsAlreadyInactive()
    {
        var targetUser = CreateTargetUser(SystemRole.Customer);

        targetUser.Suspend("Old reason");

        SetupTargetUserInDb(targetUser);
        SetupCurrentUser();
        var command = new SuspendUserCommand(targetUser.Id, "Violation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already suspended");
    }

    #endregion

    #region 3. Success Paths 

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAdminSuspendsCustomer()
    {
        var targetUser = CreateTargetUser(SystemRole.Customer);
        SetupTargetUserInDb(targetUser);
        SetupCurrentUser();

        var reason = "Spamming comments";
        var command = new SuspendUserCommand(targetUser.Id, reason);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.IsActive.Should().BeFalse(); 
        targetUser.SuspensionReason.Should().Be(reason);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSuperAdminSuspendsAdmin()
    {
        var targetUser = CreateTargetUser(SystemRole.Admin);
        SetupTargetUserInDb(targetUser);
        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.SuperAdmin.ToString());


        var command = new SuspendUserCommand(targetUser.Id, "Security breach");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.IsActive.Should().BeFalse();
    }

    #endregion
}