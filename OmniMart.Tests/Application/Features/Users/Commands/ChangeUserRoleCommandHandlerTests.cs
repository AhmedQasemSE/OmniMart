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

public class ChangeUserRoleCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<ChangeUserRoleCommandHandler>> _loggerMock = new();

    private readonly ChangeUserRoleCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();

    public ChangeUserRoleCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new ChangeUserRoleCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.SuperAdmin.ToString());
    }

    private void SetupTargetUserInDb(User targetUser)
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(targetUser.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(targetUser);
    }

    #endregion

    #region 1. Basic Security Tests

    [Theory]
    [InlineData("Customer")]
    [InlineData("Vendor")]
    [InlineData("Manager")]
    [InlineData("")]
    public async Task Handle_ShouldReturnFailure_WhenCallerIsNotAdminOrSuperAdmin(string invalidRole)
    {
        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.Role).Returns(invalidRole);

        var command = new ChangeUserRoleCommand(Guid.NewGuid(), SystemRole.Manager);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserAttemptsToChangeOwnRole()
    {
        SetupCurrentUser();

        var targetUser = new UserBuilder().WithId(_defaultUserId).WithRole(SystemRole.SuperAdmin).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, SystemRole.Admin);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("cannot change your own role");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTargetIsSuperAdmin()
    {
        SetupCurrentUser(); 

        var targetUser = new UserBuilder().WithRole(SystemRole.SuperAdmin).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, SystemRole.Admin);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("SuperAdmin account cannot be modified");
    }

    #endregion

    #region 2. Admin Specific Restrictions

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAdminAttemptsToModifyAnotherAdmin()
    {
        SetupCurrentUser();

        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString());

        var targetUser = new UserBuilder().WithRole(SystemRole.Admin).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, SystemRole.Manager);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("not have permission to modify another Admin");
    }

    [Theory]
    [InlineData(SystemRole.Admin)]
    [InlineData(SystemRole.SuperAdmin)]
    public async Task Handle_ShouldReturnFailure_WhenAdminAttemptsToGrantHighPrivileges(SystemRole highPrivilegeRole)
    {
        SetupCurrentUser();

        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString());

        var targetUser = new UserBuilder().WithRole(SystemRole.Manager).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, highPrivilegeRole);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("not have permission to grant Admin or SuperAdmin");
    }

    #endregion

    #region 3. Domain Business Rules (Staff Transitions)

    [Theory]
    [InlineData(SystemRole.Customer, SystemRole.Manager)]
    [InlineData(SystemRole.Vendor, SystemRole.SupportAgent)]
    public async Task Handle_ShouldReturnFailure_WhenUpgradingCustomerOrVendorToStaffDirectly(SystemRole currentRole, SystemRole newRole)
    {
        SetupCurrentUser(); 

        var targetUser = new UserBuilder().WithRole(currentRole).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, newRole);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Cannot change a Customer/Vendor to Staff directly");
    }

    [Theory]
    [InlineData(SystemRole.Manager, SystemRole.Customer)]
    [InlineData(SystemRole.SupportAgent, SystemRole.Vendor)]
    public async Task Handle_ShouldReturnFailure_WhenRevertingStaffToCustomerOrVendor(SystemRole currentRole, SystemRole newRole)
    {
        SetupCurrentUser();

        var targetUser = new UserBuilder().WithRole(currentRole).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, newRole);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Cannot revert a Staff member");
    }

    #endregion

    #region 4. Success Paths

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSuperAdminChangesStaffRole()
    {
        SetupCurrentUser(); 

        var targetUser = new UserBuilder().WithRole(SystemRole.Manager).Build();
        SetupTargetUserInDb(targetUser);

        var command = new ChangeUserRoleCommand(targetUser.Id, SystemRole.Admin);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.Role.Should().Be(SystemRole.Admin);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}