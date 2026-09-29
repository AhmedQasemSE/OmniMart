using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Customers.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Commands;

public class RegisterCustomerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasherService> _passwordHasherMock;
    private readonly Mock<ILogger<RegisterCustomerCommandHandler>> _loggerMock;

    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICustomerProfileRepository> _profileRepoMock;

    private readonly RegisterCustomerCommandHandler _handler;

    public RegisterCustomerCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasherService>();
        _loggerMock = new Mock<ILogger<RegisterCustomerCommandHandler>>();

        _userRepoMock = new Mock<IUserRepository>();
        _profileRepoMock = new Mock<ICustomerProfileRepository>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_profileRepoMock.Object);

        _handler = new RegisterCustomerCommandHandler(
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods 

    private RegisterCustomerCommand CreateTestCommand(
        string email = "test@omnimart.com",
        string phone = "+905555555555")
    {
        return new RegisterCustomerCommand(
            "Ahmed",
            "Salem",
            email,
            phone,
            "StrongP@ssw0rd!"
        );
    }

    private void SetupSuccessfulMocks()
    {
        _userRepoMock.Setup(repo => repo.IsEmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

        _userRepoMock.Setup(repo => repo.IsPhoneNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>()))
                           .Returns("hashed_dummy_password");

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(1); 
    }

    #endregion

    #region Tests 

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAllDataIsValid()
    {
        var command = CreateTestCommand();
        SetupSuccessfulMocks();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _userRepoMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
        _profileRepoMock.Verify(repo => repo.AddAsync(It.IsAny<CustomerProfile>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var command = CreateTestCommand();
        SetupSuccessfulMocks();

        _userRepoMock.Setup(repo => repo.IsEmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("email already exists");

        _userRepoMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPhoneNumberAlreadyExists()
    {
        var command = CreateTestCommand();
        SetupSuccessfulMocks();

        _userRepoMock.Setup(repo => repo.IsPhoneNumberExistsAsync(command.PhoneNumber, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Phone number already exists");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDbUpdateExceptionOccurs()
    {
        var command = CreateTestCommand();
        SetupSuccessfulMocks();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateException("Simulated Database Conflict"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("system conflict during registration");
    }

    #endregion
}