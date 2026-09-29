using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Features.Auth.Commands.ForgotPassword;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Events;
using OmniMart.Tests.Builders;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _loggerMock = new();

    private readonly ForgotPasswordCommandHandler _handler;
    private readonly User _defaultUser;

    public ForgotPasswordCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new ForgotPasswordCommandHandler(
            _unitOfWorkMock.Object,
            _mediatorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);

        _defaultUser = new UserBuilder().WithEmail("test@omnimart.com").Build();
    }

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithoutPublishingEvent_WhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var command = new ForgotPasswordCommand("unknown@omnimart.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mediatorMock.Verify(m => m.Publish(It.IsAny<PasswordResetRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldGenerateOtp_SaveToCache_AndPublishEvent_WhenUserExists()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@omnimart.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultUser);

        var command = new ForgotPasswordCommand("test@omnimart.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _cacheMock.Verify(c => c.SetAsync(
            $"ResetToken_test@omnimart.com",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mediatorMock.Verify(m => m.Publish(It.IsAny<PasswordResetRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}