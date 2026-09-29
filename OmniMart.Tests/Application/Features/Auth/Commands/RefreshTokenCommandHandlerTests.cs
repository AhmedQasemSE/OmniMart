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
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock;
    private readonly Mock<IUserRepository> _userRepoMock;

    private readonly RefreshTokenCommandHandler _handler;

    private readonly string _validOldRefreshToken = "old-valid-refresh-token";
    private readonly string _validNewAccessToken = "new-access-token-jwt";
    private readonly string _validNewRefreshToken = "new-valid-refresh-token";
    private readonly User _defaultUser;

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();
        _userRepoMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new RefreshTokenCommandHandler(
            _unitOfWorkMock.Object,
            _jwtProviderMock.Object,
            _loggerMock.Object);

        _defaultUser = new UserBuilder()
            .WithFirstName("Ahmed")
            .WithLastName("Yaseen")
            .WithEmail("ahmed@omnimart.com")
            .WithRole(SystemRole.Customer)
            .Build();
    }

    #region Helper Methods

    private RefreshTokenCommand CreateValidCommand()
    {
        return new RefreshTokenCommand(_validOldRefreshToken);
    }

    private void SetupValidContext()
    {
        _defaultUser.UpdateRefreshToken(_validOldRefreshToken, DateTimeOffset.UtcNow.AddDays(7));

        _userRepoMock.Setup(u => u.GetAsync(
            It.IsAny<Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<User, object>>[]>()))
            .ReturnsAsync(_defaultUser);

        _jwtProviderMock.Setup(j => j.GenerateJwtToken(_defaultUser)).Returns(_validNewAccessToken);
        _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns(_validNewRefreshToken);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenTokenIsValid_ShouldReturnNewTokensAndSave()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(_validNewAccessToken);
        result.Value.RefreshToken.Should().Be(_validNewRefreshToken);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserWithTokenNotFound_ShouldReturnUnauthorized()
    {
        SetupValidContext();

        _userRepoMock.Setup(u => u.GetAsync(
            It.IsAny<Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Invalid refresh token");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ShouldReturnUnauthorized()
    {
        SetupValidContext();

        _defaultUser.UpdateRefreshToken(_validOldRefreshToken, DateTimeOffset.UtcNow.AddDays(-1));

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("has expired");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}