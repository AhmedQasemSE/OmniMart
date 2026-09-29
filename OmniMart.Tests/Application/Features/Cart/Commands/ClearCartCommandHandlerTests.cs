using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Cart.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Cart.Commands;

public class ClearCartCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<ILogger<ClearCartCommandHandler>> _loggerMock = new();

    private readonly ClearCartCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly OmniMart.Domain.Entities.Cart _defaultCart;
    private readonly ClearCartCommand _defaultCommand = new();

    public ClearCartCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);

        _handler = new ClearCartCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);

        _defaultCart = new CartBuilder()
            .WithCustomerId(_defaultCustomerId)
            .Build();
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());

        _customerRepoMock.Setup(u => u.GetCustomerIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCustomerId);

        _cartRepoMock.Setup(c => c.GetActiveCartByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultCart);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(string.Empty);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized); 
    }

    [Fact]
    public async Task Handle_WhenCustomerProfileIsNull_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();

        _customerRepoMock.Setup(u => u.GetCustomerIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Guid?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("profile not found");
    }

    [Fact]
    public async Task Handle_WhenActiveCartIsNull_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();

        _cartRepoMock.Setup(c => c.GetActiveCartByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((OmniMart.Domain.Entities.Cart?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("cart found"); 
    }

    [Fact]
    public async Task Handle_WhenAllConditionsAreMet_ShouldClearCartAndSave()
    {
        SetupDefaultSuccessBehavior();

        _defaultCart.AddItem(Guid.NewGuid(), 5);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultCart.CartItems.Should().BeEmpty(); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}