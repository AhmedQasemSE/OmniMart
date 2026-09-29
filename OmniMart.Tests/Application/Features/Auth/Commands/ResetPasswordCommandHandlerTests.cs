using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Auth.Command;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<ResetPasswordCommandHandler>> _loggerMock = new();

    private readonly ResetPasswordCommandHandler _handler;
    private readonly User _defaultUser;

    public ResetPasswordCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new ResetPasswordCommandHandler(
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);

        _defaultUser = new UserBuilder().WithEmail("test@omnimart.com").Build();
    }

    #region Helper Methods

    private void SetupValidCacheToken(string email, string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        _cacheMock.Setup(c => c.GetAsync($"ResetToken_{email}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(tokenBytes);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var command = new ResetPasswordCommand("wrong@omnimart.com", "123456", "NewPass", "NewPass");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenTokenIsMissingOrExpired()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@omnimart.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultUser);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);

        var command = new ResetPasswordCommand("test@omnimart.com", "123456", "NewPass", "NewPass");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("incorrect or expired");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenTokenDoesNotMatch()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@omnimart.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultUser);

        SetupValidCacheToken("test@omnimart.com", "654321"); 

        var command = new ResetPasswordCommand("test@omnimart.com", "123456", "NewPass", "NewPass"); 
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndRemoveToken_WhenTokenMatches()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@omnimart.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultUser);

        SetupValidCacheToken("test@omnimart.com", "123456");

        var command = new ResetPasswordCommand("test@omnimart.com", "123456", "NewPass", "NewPass");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _cacheMock.Verify(c => c.RemoveAsync($"ResetToken_test@omnimart.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}