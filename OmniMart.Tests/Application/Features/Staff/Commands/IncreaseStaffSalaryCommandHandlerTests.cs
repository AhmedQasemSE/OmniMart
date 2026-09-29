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

public class IncreaseStaffSalaryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStaffProfileRepository> _staffRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<IncreaseStaffSalaryCommandHandler>> _loggerMock = new();

    private readonly IncreaseStaffSalaryCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public IncreaseStaffSalaryCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.StaffProfiles).Returns(_staffRepoMock.Object);

        _handler = new IncreaseStaffSalaryCommandHandler(
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

        var command = new IncreaseStaffSalaryCommand(Guid.NewGuid(), 1000m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenStaffProfileDoesNotExist()
    {
        SetupCurrentUser();
        SetupStaffDb(null); 

        var command = new IncreaseStaffSalaryCommand(Guid.NewGuid(), 1000m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenUserTriesToIncreaseOwnSalary()
    {
        SetupCurrentUser();

        var staffProfile = new StaffProfileBuilder()
            .WithUserId(_defaultUserId) 
            .Build();

        SetupStaffDb(staffProfile);

        var command = new IncreaseStaffSalaryCommand(_defaultUserId, 1000m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("cannot modify your own salary");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainThrowsArgumentException()
    {
        SetupCurrentUser();

        var staffProfile = new StaffProfileBuilder().WithSalary(5000m).Build();
        SetupStaffDb(staffProfile);

        var command = new IncreaseStaffSalaryCommand(staffProfile.UserId, -500m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenConcurrencyExceptionOccurs()
    {
        SetupCurrentUser();
        var staffProfile = new StaffProfileBuilder().Build();
        SetupStaffDb(staffProfile);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        var command = new IncreaseStaffSalaryCommand(staffProfile.UserId, 1000m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by another user");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndIncreaseSalary_WhenDataIsValid()
    {
        SetupCurrentUser();

        var staffProfile = new StaffProfileBuilder().WithSalary(5000m).Build();
        SetupStaffDb(staffProfile);

        var command = new IncreaseStaffSalaryCommand(staffProfile.UserId, 1500m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        staffProfile.Salary.Should().Be(6500m);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}