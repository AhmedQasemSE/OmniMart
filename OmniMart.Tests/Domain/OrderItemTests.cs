using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class OrderItemTests
{
    private OrderItem CreateTestOrderItem(
        Guid? orderId = null,
        Guid? productVariantId = null,
        decimal price = 150m,
        int quantity = 2)
    {
        return new OrderItem(
            orderId ?? Guid.NewGuid(),
            productVariantId ?? Guid.NewGuid(),
            price,
            quantity);
    }

    [Fact]
    public void Constructor_ShouldCreateOrderItem_WhenValidArgumentsProvided()
    {
        var orderId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var orderItem = CreateTestOrderItem(orderId, variantId, 150m, 2);

        orderItem.Should().NotBeNull();
        orderItem.OrderId.Should().Be(orderId);
        orderItem.ProductVariantId.Should().Be(variantId);
        orderItem.Price.Should().Be(150m);
        orderItem.Quantity.Should().Be(2);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenOrderIdIsEmpty()
    {
        Action act = () => CreateTestOrderItem(orderId: Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*Order  ID cannot be empty.*");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenProductVariantIdIsEmpty()
    {
        Action act = () => CreateTestOrderItem(productVariantId: Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*ProductVariantId  ID cannot be empty.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_ShouldThrowException_WhenPriceIsZeroOrNegative(decimal invalidPrice)
    {
        Action act = () => CreateTestOrderItem(price: invalidPrice);

        act.Should().Throw<ArgumentException>().WithMessage("*Price cannot be negative.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowException_WhenQuantityIsZeroOrNegative(int invalidQuantity)
    {
        Action act = () => CreateTestOrderItem(quantity: invalidQuantity);

        act.Should().Throw<ArgumentException>().WithMessage("*Quantity must be greater than zero.*");
    }
}