using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Cart.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Cart.Commands;

public class AddToCartCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<ILogger<AddToCartCommandHandler>> _loggerMock = new();

    private readonly AddToCartCommandHandler _handler;


    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly ProductVariant _defaultVariant;
    private readonly OmniMart.Domain.Entities.Cart _defaultCart;
    private readonly AddToCartCommand _defaultCommand;

    public AddToCartCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);

        _handler = new AddToCartCommandHandler(
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        _defaultVariant = new ProductVariantBuilder()
            .WithStockQuantity(20)
            .WithValidActiveProductAndVendor() 
            .Build();

        _defaultCart = new CartBuilder()
            .WithCustomerId(_defaultCustomerId)
            .Build();

        _defaultCommand = new AddToCartCommand(_defaultVariant.Id, 5);
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());

        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCustomerId);

        _productRepoMock.Setup(r => r.GetVariantByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_defaultVariant);

        _cartRepoMock.Setup(r => r.GetActiveCartByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_defaultCart);
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
        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Guid?)null); 

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductVariantIsNull_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _productRepoMock.Setup(r => r.GetVariantByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ProductVariant?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Theory]
    [InlineData(false, ProductStatus.Active, true)]
    [InlineData(true, ProductStatus.Draft, true)]
    [InlineData(true, ProductStatus.Active, false)]
    public async Task Handle_WhenProductIsInvalid_ShouldReturnValidation(bool isPublished, ProductStatus status, bool isVendorApproved)
    {
        SetupDefaultSuccessBehavior();

        var invalidVariant = new ProductVariantBuilder()
            .WithCustomProductStatus(isPublished, status, isVendorApproved)
            .Build();

        _productRepoMock.Setup(r => r.GetVariantByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(invalidVariant); 

        var command = new AddToCartCommand(invalidVariant.Id, 4);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenRequestedQuantityExceedsStock_ShouldReturnConflict()
    {
        SetupDefaultSuccessBehavior();
        var command = new AddToCartCommand(_defaultVariant.Id, 50); 

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Not enough stock");
    }

    [Fact]
    public async Task Handle_WhenRequestedQuantityExceedsAvailableStock_DueToReservations_ShouldReturnConflict()
    {
        SetupDefaultSuccessBehavior();
        _defaultVariant.ReserveStock(15); 

        var command = new AddToCartCommand(_defaultVariant.Id, 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Not enough stock");
    }

    [Fact]
    public async Task Handle_WhenItemAlreadyInCart_AndCumulativeQuantityExceedsAvailableStock_ShouldReturnConflict()
    {
        SetupDefaultSuccessBehavior();

        _defaultCart.AddItem(_defaultVariant.Id, 16);

        var command = new AddToCartCommand(_defaultVariant.Id, 5); 
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already have 16 in cart"); 
    }

    [Fact]
    public async Task Handle_WhenAllConditionsAreMet_ShouldAddToCartAndSave()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(_defaultCart.Id);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}