using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Orders.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Commands;

public class CheckoutCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<ILogger<CheckoutCommandHandler>> _loggerMock = new();

    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IPaymentGroupRepository> _paymentGroupRepoMock = new();

    private readonly CheckoutCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly Guid _defaultAddressId;

    private readonly CustomerProfile _defaultCustomer;
    private readonly ProductVariant _defaultVariant;
    private readonly OmniMart.Domain.Entities.Cart _defaultCart; 
    private readonly CheckoutCommand _defaultCommand;

    public CheckoutCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.PaymentGroups).Returns(_paymentGroupRepoMock.Object);

        _handler = new CheckoutCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _paymentServiceMock.Object,
            _loggerMock.Object);

        _defaultCustomer = new CustomerProfileBuilder()
            .WithId(_defaultCustomerId)
            .WithUserId(_defaultUserId)
            .Build();
        _defaultAddressId = _defaultCustomer.AddAddress("Home", "City", "Street", "123", "123456");

        _defaultVariant = new ProductVariantBuilder()
            .WithValidActiveProductAndVendor()
            .WithStockQuantity(50)
            .WithPrice(100m)
            .Build();

        var cartItem = new CartItemBuilder()
            .WithQuantity(2)
            .WithProductVariantEntity(_defaultVariant)
            .Build();

        _defaultCart = new CartBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithCartItem(cartItem)
            .Build();

        _defaultCommand = new CheckoutCommand(_defaultAddressId, PaymentMethod.CreditCard.ToString(), "txn_12345");
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());

        _customerRepoMock.Setup(x => x.GetAsync(
            It.IsAny<Expression<Func<CustomerProfile, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<CustomerProfile, object>>>()))
            .ReturnsAsync(_defaultCustomer);

        _cartRepoMock.Setup(x => x.GetActiveCartByCustomerIdAsync(_defaultCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultCart);

        _orderRepoMock.Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _paymentGroupRepoMock.Setup(x => x.AddAsync(It.IsAny<PaymentGroup>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _paymentServiceMock.Setup(x => x.CreateCheckoutSessionAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync("https://stripe.com/fake-checkout");
    }

    #endregion

    #region 1. Validation & Failure Tests

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsInvalid()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(string.Empty);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCustomerProfileDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        _customerRepoMock.Setup(x => x.GetAsync(
            It.IsAny<Expression<Func<CustomerProfile, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<CustomerProfile, object>>>()))
            .ReturnsAsync((CustomerProfile?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Customer profile not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAddressDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        var commandWithWrongAddress = new CheckoutCommand(Guid.NewGuid(), PaymentMethod.CreditCard.ToString());

        var result = await _handler.Handle(commandWithWrongAddress, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("address not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenCartIsEmpty()
    {
        SetupDefaultSuccessBehavior();
        var emptyCart = new CartBuilder().WithCustomerId(_defaultCustomerId).Build();
        _cartRepoMock.Setup(x => x.GetActiveCartByCustomerIdAsync(_defaultCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCart);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("cart is empty");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenPaymentMethodIsInvalid()
    {
        SetupDefaultSuccessBehavior();
        var commandWithBadPayment = new CheckoutCommand(_defaultAddressId, "InvalidMethod");

        var result = await _handler.Handle(commandWithBadPayment, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Invalid payment method");
    }

    [Fact]
    public async Task Handle_ShouldRollbackTransaction_WhenDatabaseThrowsException()
    {
        SetupDefaultSuccessBehavior();
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection lost!"));

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);

        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductIsNoLongerAvailable()
    {
        SetupDefaultSuccessBehavior();
        _defaultVariant.Product!.SetPublishStatus(false);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
        result.ErrorMessage.Should().Contain("error occurred during checkout");

        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductIsOutOfStock()
    {
        SetupDefaultSuccessBehavior();
        _defaultVariant.ReserveStock(50);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
    #region 2. Success & Payment Tests

    [Fact]
    public async Task Handle_ShouldCreateOrderAndReturnPaymentUrl_WhenCheckoutIsSuccessful()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderIds.Should().NotBeEmpty();
        result.Value.PaymentUrl.Should().Be("https://stripe.com/fake-checkout");

        _defaultCart.CartItems.Should().BeEmpty();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCommitOrderWithEmptyUrl_WhenStripeThrowsException()
    {
        SetupDefaultSuccessBehavior();

        _paymentServiceMock.Setup(x => x.CreateCheckoutSessionAsync(
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Stripe API is down"));

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderIds.Should().NotBeEmpty();
        result.Value.PaymentUrl.Should().Be(string.Empty);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateMultipleOrders_WhenCartHasItemsFromDifferentVendors()
    {
        SetupDefaultSuccessBehavior();

        var variantFromAnotherVendor = new ProductVariantBuilder()
            .WithValidActiveProductAndVendor() 
            .WithStockQuantity(30)
            .WithPrice(200m)
            .Build();

        var cartItemFromAnotherVendor = new CartItemBuilder()
            .WithQuantity(1)
            .WithProductVariantEntity(variantFromAnotherVendor)
            .Build();

        var firstCartItem = new CartItemBuilder()
            .WithQuantity(2)
            .WithProductVariantEntity(_defaultVariant)
            .Build();

        var multiVendorCart = new CartBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithCartItem(firstCartItem)
            .WithCartItem(cartItemFromAnotherVendor)
            .Build();

        _cartRepoMock.Setup(x => x.GetActiveCartByCustomerIdAsync(_defaultCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(multiVendorCart);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value.OrderIds.Count.Should().Be(2);

        _unitOfWorkMock.Verify(x => x.Orders.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}