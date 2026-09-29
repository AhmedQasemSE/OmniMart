using FluentAssertions;
using OmniMart.Domain.Common;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class PaymentGroupTests
{
    private PaymentGroup CreateTestPaymentGroup(Guid? customerId = null)
    {
        return new PaymentGroup(customerId ?? Guid.NewGuid());
    }

    private Order CreateTestOrder(decimal amount)
    {
        return new OrderBuilder()
            .WithForcedTotalAmount(amount)
            .Build();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreatePaymentGroup_WhenCustomerIdIsValid()
    {
        var customerId = Guid.NewGuid();
        var paymentGroup = CreateTestPaymentGroup(customerId);

        paymentGroup.Should().NotBeNull();
        paymentGroup.Id.Should().NotBeEmpty();
        paymentGroup.CustomerId.Should().Be(customerId);
        paymentGroup.TotalAmount.Should().Be(0);
        paymentGroup.IsPaid.Should().BeFalse();
        paymentGroup.Orders.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenCustomerIdIsEmpty()
    {
        Action act = () => CreateTestPaymentGroup(Guid.Empty);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer ID cannot be empty*");
    }

    #endregion

    #region AddOrder Tests

    [Fact]
    public void AddOrder_ShouldAddOrderToCollectionAndIncreaseTotalAmount()
    {
        var paymentGroup = CreateTestPaymentGroup();
        var order1 = CreateTestOrder(100.50m);
        var order2 = CreateTestOrder(200.00m);

        paymentGroup.AddOrder(order1);
        paymentGroup.AddOrder(order2);

        paymentGroup.Orders.Should().HaveCount(2);
        paymentGroup.TotalAmount.Should().Be(300.50m); 
    }

    #endregion

    #region Payment Process Tests

    [Fact]
    public void MarkAsPaid_ShouldSetPaidStatusAndUpdateAllOrders()
    {
        var paymentGroup = CreateTestPaymentGroup();
        var order1 = CreateTestOrder(100m);
        paymentGroup.AddOrder(order1);

        var stripeSessionId = "txn_success_123";

        paymentGroup.MarkAsPaid(stripeSessionId);

        paymentGroup.IsPaid.Should().BeTrue();
        paymentGroup.StripeSessionId.Should().Be(stripeSessionId);

        order1.Status.Should().Be(OrderStatus.Processing);
        order1.PaymentTransactionId.Should().Be(stripeSessionId);
    }

    [Fact]
    public void MarkAsFailed_ShouldCancelAllOrders_WhenPaymentIsNotPaid()
    {
        var paymentGroup = CreateTestPaymentGroup();
        var order = CreateTestOrder(100m);
        paymentGroup.AddOrder(order);

        paymentGroup.MarkAsFailed();

        paymentGroup.IsPaid.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void MarkAsFailed_ShouldDoNothing_WhenPaymentIsAlreadyPaid()
    {
        var paymentGroup = CreateTestPaymentGroup();
        var order = CreateTestOrder(100m);
        paymentGroup.AddOrder(order);

        paymentGroup.MarkAsPaid("txn_123");

        paymentGroup.MarkAsFailed();

        paymentGroup.IsPaid.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Processing); 
    }

    #endregion
}