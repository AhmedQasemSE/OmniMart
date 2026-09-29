using FluentAssertions;
using OmniMart.Domain.Common;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Domain.Events;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class OrderTests
{
    private readonly Guid _defaultCustomerId;
    private readonly Guid _defaultVendorId;
    private readonly Guid _defaultPaymentGroupId;
    private readonly Address _defaultAddress;

    private readonly Order _order; 

    public OrderTests()
    {
        _defaultCustomerId = Guid.NewGuid();
        _defaultVendorId = Guid.NewGuid();
        _defaultPaymentGroupId = Guid.NewGuid();
        _defaultAddress = new Address("Istanbul", "Istiklal", "34000", "0555555555");

        _order = new Order(_defaultCustomerId, _defaultVendorId, _defaultPaymentGroupId, PaymentMethod.CreditCard, _defaultAddress);
    }

    #region 1. Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateOrder_WhenValidArgumentsProvided()
    {
        var order = new Order(_defaultCustomerId, _defaultVendorId, _defaultPaymentGroupId, PaymentMethod.CreditCard, _defaultAddress);

        order.Should().NotBeNull();
        order.CustomerId.Should().Be(_defaultCustomerId);
        order.VendorId.Should().Be(_defaultVendorId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenCustomerIdIsEmpty()
    {
        Action act = () => new Order(Guid.Empty, _defaultVendorId, _defaultPaymentGroupId, PaymentMethod.CreditCard, _defaultAddress);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer ID cannot be empty*");
    }

    #endregion

    #region 2. ChangeOrderStatus Tests

    [Fact]
    public void ChangeOrderStatus_ShouldThrowException_WhenOrderIsAlreadyCancelled()
    {
        _order.ChangeOrderStatus(OrderStatus.Cancelled);

        Action act = () => _order.ChangeOrderStatus(OrderStatus.Shipped);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot change the status of a Cancelled or Refunded order!");
    }

    [Fact]
    public void ChangeOrderStatus_ShouldChangeStatusAndAddEvent_WhenStatusIsRefunded()
    {
        _order.ChangeOrderStatus(OrderStatus.Refunded);

        _order.Status.Should().Be(OrderStatus.Refunded);

        _order.DomainEvents.Should().ContainSingle();
        var domainEvent = _order.DomainEvents.First();
        domainEvent.Should().BeOfType<OrderRefundedEvent>();

        var refundedEvent = (OrderRefundedEvent)domainEvent;
        refundedEvent.OrderId.Should().Be(_order.Id);
        refundedEvent.CustomerId.Should().Be(_defaultCustomerId);
        refundedEvent.RefundAmount.Should().Be(_order.TotalAmount);
    }

    [Fact]
    public void ChangeOrderStatus_ShouldChangeStatusAndAddEvent_WhenStatusIsCancelled()
    {
        _order.ChangeOrderStatus(OrderStatus.Cancelled);

        _order.Status.Should().Be(OrderStatus.Cancelled);

        _order.DomainEvents.Should().ContainSingle();
        var domainEvent = _order.DomainEvents.First();
        domainEvent.Should().BeOfType<OrderCancelledEvent>();

        var cancelledEvent = (OrderCancelledEvent)domainEvent;
        cancelledEvent.OrderId.Should().Be(_order.Id);
        cancelledEvent.CustomerId.Should().Be(_defaultCustomerId);
        cancelledEvent.TotalAmount.Should().Be(_order.TotalAmount);
    }

    [Fact]
    public void ChangeOrderStatus_ShouldChangeStatusAndAddEvent_WhenStatusIsPending()
    {
        _order.ChangeOrderStatus(OrderStatus.Pending);

        _order.Status.Should().Be(OrderStatus.Pending);

        _order.DomainEvents.Should().ContainSingle();
        _order.DomainEvents.First().Should().BeOfType<OrderPlacedEvent>();
    }

    [Fact]
    public void ChangeOrderStatus_ShouldChangeStatusAndAddEvent_WhenStatusIsDelivered()
    {
        _order.ChangeOrderStatus(OrderStatus.Delivered);

        _order.Status.Should().Be(OrderStatus.Delivered);

        _order.DomainEvents.Should().ContainSingle();
        var domainEvent = _order.DomainEvents.First();
        domainEvent.Should().BeOfType<OrderDeliveredEvent>();

        var deliveredEvent = (OrderDeliveredEvent)domainEvent;
        deliveredEvent.OrderId.Should().Be(_order.Id);
        deliveredEvent.CustomerId.Should().Be(_defaultCustomerId);
    }

    #endregion

    #region 3. AddOrderItem Tests

    [Fact]
    public void AddOrderItem_ShouldThrowException_WhenProductVariantIdIsEmpty()
    {
        Action action = () => _order.AddOrderItem(Guid.Empty, 500, 4);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*Product Variant ID cannot be empty*");
    }

    [Fact]
    public void AddOrderItem_ShouldThrowException_WhenPriceIsNegative()
    {
        var productVariantId = Guid.NewGuid();

        Action action = () => _order.AddOrderItem(productVariantId, -500, 4);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*cannot be negative.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddOrderItem_ShouldThrowException_WhenQuantityIsNegativeOrZero(int quantity)
    {
        var productVariantId = Guid.NewGuid();

        Action action = () => _order.AddOrderItem(productVariantId, 500, quantity);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void AddOrderItem_ShouldAddItemAndIncreaseTotal_WhenDataIsValid()
    {
        var productVariantId = Guid.NewGuid();

        _order.AddOrderItem(productVariantId, 500, 4);

        _order.OrderItems.Should().HaveCount(1);

        var addedItem = _order.OrderItems.First();
        addedItem.ProductVariantId.Should().Be(productVariantId);
        addedItem.Price.Should().Be(500);
        addedItem.Quantity.Should().Be(4);

        _order.TotalAmount.Should().Be(2000);
    }

    #endregion

    #region 4. SetTransactionId Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetTransactionId_ShouldThrowException_WhenTransactionIdIsNullOrWhiteSpace(string? invalidTransactionId)
    {
        Action act = () => _order.SetTransactionId(invalidTransactionId!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Transaction ID cannot be empty*");
    }

    [Fact]
    public void SetTransactionId_ShouldSetId_WhenTransactionIdIsValid()
    {
        var transactionId = "txn_123456789_stripe";

        _order.SetTransactionId(transactionId);

        _order.PaymentTransactionId.Should().Be(transactionId);
    }

    #endregion
} 
