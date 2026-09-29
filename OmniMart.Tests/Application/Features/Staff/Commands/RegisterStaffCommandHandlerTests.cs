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
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Commands;

public class RegisterStaffCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IStaffProfileRepository> _staffRepoMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<ILogger<RegisterStaffCommandHandler>> _loggerMock = new();

    private readonly RegisterStaffCommandHandler _handler;

    public RegisterStaffCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.StaffProfiles).Returns(_staffRepoMock.Object);

        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("HashedPassword123!");

        _handler = new RegisterStaffCommandHandler(
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private RegisterStaffCommand CreateValidCommand()
    {
        return new RegisterStaffCommand(
            "Samir",
            "Nassar",
            "samir.hr@omnimart.com",
            "0555987654",
            "StrongPass123!",
            Department.HR,
            SystemRole.Manager,
            7500m
        );
    }

    private void SetupNoConflicts()
    {
        _userRepoMock.Setup(r => r.IsEmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

        _userRepoMock.Setup(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailExists()
    {
        var command = CreateValidCommand();
        SetupNoConflicts();

        _userRepoMock.Setup(r => r.IsEmailExistsAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("email already exists");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPhoneNumberExists()
    {
        var command = CreateValidCommand();
        SetupNoConflicts();

        _userRepoMock.Setup(r => r.IsPhoneNumberExistsAsync(command.PhoneNumber, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Phone number already exists");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndCreateEntities_WhenDataIsValid()
    {
        var command = CreateValidCommand();
        SetupNoConflicts();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty(); 

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == command.Email &&
            u.Role == SystemRole.Manager &&
            u.AccountNumber.StartsWith("ACC_")
        ), It.IsAny<CancellationToken>()), Times.Once);

        _staffRepoMock.Verify(r => r.AddAsync(It.Is<StaffProfile>(s =>
            s.DepartmentRole == command.DepartmentRole &&
            s.Salary == command.Salary &&
            s.StaffNumber.StartsWith("STF_")
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDbUpdateExceptionOccurs()
    {
        var command = CreateValidCommand();
        SetupNoConflicts();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateException());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("system conflict");
    }

    #endregion
}