using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Cart.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Cart.Commands;

public class UpdateCartItemQuantityCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ILogger<UpdateCartItemQuantityCommandHandler>> _loggerMock = new();

    private readonly UpdateCartItemQuantityCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly ProductVariant _defaultVariant;
    private readonly OmniMart.Domain.Entities.Cart _defaultCart;
    private readonly UpdateCartItemQuantityCommand _defaultCommand;

    public UpdateCartItemQuantityCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new UpdateCartItemQuantityCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);

        _defaultVariant = new ProductVariantBuilder()
            .WithStockQuantity(50)
            .Build();

        _defaultCart = new CartBuilder()
            .WithCustomerId(_defaultCustomerId)
            .Build();
        _defaultCart.AddItem(_defaultVariant.Id, 1);

        _defaultCommand = new UpdateCartItemQuantityCommand(_defaultVariant.Id, 5);
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());

        _customerRepoMock.Setup(c => c.GetCustomerIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCustomerId);

        _cartRepoMock.Setup(c => c.GetActiveCartByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultCart);

        _productRepoMock.Setup(p => p.GetVariantByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_defaultVariant);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(u => u.UserId).Returns(string.Empty); 

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenCustomerProfileIsNull_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _customerRepoMock.Setup(c => c.GetCustomerIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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
        _cartRepoMock.Setup(ca => ca.GetActiveCartByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((OmniMart.Domain.Entities.Cart?)null); 

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("active cart found"); 
    }

    [Fact]
    public async Task Handle_WhenItemNotInCart_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();

        var commandNotInCart = new UpdateCartItemQuantityCommand(Guid.NewGuid(), 5);

        var result = await _handler.Handle(commandNotInCart, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("not in your cart");
    }

    [Fact]
    public async Task Handle_WhenProductVariantIsNull_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _productRepoMock.Setup(p => p.GetVariantByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ProductVariant?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("variant not found"); 
    }

    [Fact]
    public async Task Handle_WhenRequestedQuantityExceedsAvailableStock_DueToReservations_ShouldReturnConflict()
    {
        SetupDefaultSuccessBehavior();
        _defaultVariant.ReserveStock(48); 


        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Not enough stock");
    }

    [Fact]
    public async Task Handle_WhenInvalidOperationExceptionThrown_ShouldReturnValidationFailure()
    {
        SetupDefaultSuccessBehavior();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Simulated unexpected error")); 

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Simulated unexpected error");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateQuantityAndSave()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _defaultVariant.StockQuantity.Should().Be(50);
        _defaultVariant.ReservedStock.Should().Be(0);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}