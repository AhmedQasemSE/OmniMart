using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Staff.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Commands;

public class TransferStaffDepartmentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStaffProfileRepository> _staffRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<TransferStaffDepartmentCommandHandler>> _loggerMock = new();

    private readonly TransferStaffDepartmentCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public TransferStaffDepartmentCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.StaffProfiles).Returns(_staffRepoMock.Object);

        _handler = new TransferStaffDepartmentCommandHandler(
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
    private void SetupStaffDb(StaffProfile? profile)
    {
        _staffRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<StaffProfile, bool>>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(profile);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    [InlineData(SystemRole.SupportAgent)]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAdmin(SystemRole role)
    {
        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.Role).Returns(role.ToString());

        var command = new TransferStaffDepartmentCommand(Guid.NewGuid(), Department.HR);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized); 
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenStaffProfileDoesNotExist()
    {
        SetupCurrentUser();
        SetupStaffDb(null);

        var command = new TransferStaffDepartmentCommand(Guid.NewGuid(), Department.HR);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound); 
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenDomainThrowsInvalidOperationException()
    {
        SetupCurrentUser();

        var staffProfile = new StaffProfileBuilder().WithDepartment(Department.IT).Build();
        SetupStaffDb(staffProfile);

        var command = new TransferStaffDepartmentCommand(staffProfile.UserId, Department.IT);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation); 
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenConcurrencyExceptionOccurs()
    {
        SetupCurrentUser();
        var staffProfile = new StaffProfileBuilder().WithDepartment(Department.IT).Build();
        SetupStaffDb(staffProfile);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        var command = new TransferStaffDepartmentCommand(staffProfile.UserId, Department.HR);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndChangeDepartment_WhenDataIsValid()
    {
        SetupCurrentUser();

        var staffProfile = new StaffProfileBuilder().WithDepartment(Department.IT).Build();
        SetupStaffDb(staffProfile);

        var command = new TransferStaffDepartmentCommand(staffProfile.UserId, Department.HR);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        staffProfile.DepartmentRole.Should().Be(Department.HR);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}