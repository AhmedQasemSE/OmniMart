using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Auth.Command;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();

    private readonly LoginCommandHandler _handler;
    private readonly User _defaultUser;

    public LoginCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new LoginCommandHandler(
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtProviderMock.Object,
            _loggerMock.Object);

        _defaultUser = new UserBuilder()
            .WithEmail("test@omnimart.com")
            .WithRole(SystemRole.Customer)
            .Build();
    }

    #region Helper Methods

    private void SetupValidCredentials()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@omnimart.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultUser);

        _passwordHasherMock.Setup(p => p.VerifyPassword("ValidPassword123!", It.IsAny<string>()))
                           .Returns(true);

        _jwtProviderMock.Setup(j => j.GenerateJwtToken(_defaultUser))
                        .Returns("mocked-jwt-token");

        _jwtProviderMock.Setup(j => j.GenerateRefreshToken())
                        .Returns("mocked-refresh-token");
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var command = new LoginCommand("wrong@omnimart.com", "Password123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPasswordIsIncorrect()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@omnimart.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultUser);

        _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPassword!", It.IsAny<string>()))
                           .Returns(false); 

        var command = new LoginCommand("test@omnimart.com", "WrongPassword!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid email or password");
    }

    
    [Fact]
    public async Task Handle_ShouldReturnAuthResponse_AndRecordLogin_WhenCredentialsAreValid()
    {
        SetupValidCredentials();

        var command = new LoginCommand("test@omnimart.com", "ValidPassword123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("mocked-jwt-token");

        result.Value.RefreshToken.Should().Be("mocked-refresh-token");

        result.Value.UserId.Should().Be(_defaultUser.Id);

        _defaultUser.LastLoginAt.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    #endregion
}