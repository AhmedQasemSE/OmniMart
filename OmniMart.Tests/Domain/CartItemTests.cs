using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain;

public class CartItemTests
{
    private readonly Guid _cartId;
    private readonly Guid _variantId;
    private readonly int _initialQuantity;
    private readonly CartItem _cartItem;

    public CartItemTests()
    {
        _cartId = Guid.NewGuid();
        _variantId = Guid.NewGuid();
        _initialQuantity = 5; 

        _cartItem = new CartItem(_cartId, _variantId, _initialQuantity);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateCartItem()
    {
        _cartItem.CartId.Should().Be(_cartId);
        _cartItem.ProductVariantId.Should().Be(_variantId);
        _cartItem.Quantity.Should().Be(_initialQuantity);
    }

    [Fact]
    public void Constructor_WhenCartIdIsEmpty_ShouldThrowArgumentException()
    {
        Action action = () => new CartItem(Guid.Empty, Guid.NewGuid(), 1);
        action.Should().Throw<ArgumentException>().WithMessage("*Cart ID cannot be empty*");
    }

    [Fact]
    public void Constructor_WhenProductVariantIdIsEmpty_ShouldThrowArgumentException()
    {
        Action action = () => new CartItem(Guid.NewGuid(), Guid.Empty, 1);
        action.Should().Throw<ArgumentException>().WithMessage("*ProductVariant ID cannot be empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenQuantityIsInvalid_ShouldThrowArgumentException(int invalidQuantity)
    {
        Action action = () => new CartItem(Guid.NewGuid(), Guid.NewGuid(), invalidQuantity);
        action.Should().Throw<ArgumentException>().WithMessage("*Quantity must be greater than zero*");
    }

    #endregion

    #region AddQuantity Tests

    [Fact]
    public void AddQuantity_WhenAmountIsValid_ShouldIncreaseQuantity()
    {
        _cartItem.AddQuantity(3);

        _cartItem.Quantity.Should().Be(8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddQuantity_WhenAmountIsInvalid_ShouldThrowArgumentException(int invalidAmount)
    {
        Action action = () => _cartItem.AddQuantity(invalidAmount);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*Quantity to add must be greater than zero*");
    }

    #endregion

    #region DecreaseQuantity Tests

    [Fact]
    public void DecreaseQuantity_WhenAmountIsValid_ShouldDecreaseQuantity()
    {
        _cartItem.DecreaseQuantity(2);

        _cartItem.Quantity.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecreaseQuantity_WhenAmountToRemoveIsInvalid_ShouldThrowArgumentException(int invalidAmount)
    {
        Action action = () => _cartItem.DecreaseQuantity(invalidAmount);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*Amount to remove must be positive*");
    }

    [Fact]
    public void DecreaseQuantity_WhenAmountIsGreaterThanCurrent_ShouldThrowInvalidOperationException()
    {
        Action action = () => _cartItem.DecreaseQuantity(10);

        action.Should().Throw<InvalidOperationException>()
              .WithMessage("*Cannot decrease quantity below zero*");
    }

    #endregion

    #region SetQuantity Tests

    [Fact]
    public void SetQuantity_WhenQuantityIsValid_ShouldUpdateQuantity()
    {
        _cartItem.SetQuantity(15);

        _cartItem.Quantity.Should().Be(15);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetQuantity_WhenQuantityIsInvalid_ShouldThrowArgumentException(int invalidQuantity)
    {
        Action action = () => _cartItem.SetQuantity(invalidQuantity);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*greater than zero*");
    }

    #endregion
}