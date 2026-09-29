using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class RegisterVendorCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<ILogger<RegisterVendorCommandHandler>> _loggerMock = new();

    private readonly RegisterVendorCommandHandler _handler;

    public RegisterVendorCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);

        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("HashedPassword123!");

        _handler = new RegisterVendorCommandHandler(
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private RegisterVendorCommand CreateValidCommand()
    {
        return new RegisterVendorCommand(
            "Ahmed",
            "Yaseen",
            "vendor@omnimart.com",
            "0555123456",
            "StrongPass123!",
            "Ahmed Tech Store",
            "CR-998877"
        );
    }

    private void SetupNoConflicts()
    {
        _vendorRepoMock.Setup(r => r.IsCommercialRegisterNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);

        _userRepoMock.Setup(r => r.IsEmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

        _userRepoMock.Setup(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCommercialRegisterNumberExists()
    {
        var command = CreateValidCommand();
        _vendorRepoMock.Setup(r => r.IsCommercialRegisterNumberExistsAsync(command.CommercialRegisterNumber, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Commercial register number already exists");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

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
            u.FirstName == command.FirstName &&
            u.Role == SystemRole.Vendor &&
            u.AccountNumber.StartsWith("ACC_")
        )), Times.Once);

        _vendorRepoMock.Verify(r => r.AddAsync(It.Is<VendorProfile>(v =>
            v.StoreName == command.StoreName &&
            v.CommercialRegisterNumber == command.CommercialRegisterNumber &&
            v.CommissionRate == 5m &&
            v.VendorNumber.StartsWith("VND_")
        )), Times.Once);

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